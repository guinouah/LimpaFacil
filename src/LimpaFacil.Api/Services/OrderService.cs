using System.Data;
using LimpaFacil.Api.Data;
using LimpaFacil.Contracts;
using LimpaFacil.Domain;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace LimpaFacil.Api.Services;

public sealed class OrderService(AppDbContext db)
{
    public async Task<Order> CheckoutAsync(Guid clientId, CheckoutRequest request, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try { return await CheckoutOnceAsync(clientId, request, ct); }
            catch (Exception ex) when (IsLockConflict(ex))
            {
                // A transação anterior já foi desfeita pelo await using. Recomeça inteira,
                // com novo estado rastreado e a mesma chave lógica para impedir duplicação.
                db.ChangeTracker.Clear();
                if (attempt == 2) throw new ConcurrencyException();
                await Task.Delay(TimeSpan.FromMilliseconds(75 * (attempt + 1)), ct);
            }
        }
        throw new ConcurrencyException();
    }

    private static bool IsLockConflict(Exception error) =>
        error is MySqlException { Number: 1213 or 1205 } || error.InnerException is not null && IsLockConflict(error.InnerException);

    private async Task<Order> CheckoutOnceAsync(Guid clientId, CheckoutRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 80)
            throw new DomainException("A chave de idempotência é obrigatória e deve ter até 80 caracteres.");
        if (request.Items.Count == 0) throw new DomainException("O carrinho está vazio.");
        if (request.Observation?.Length > 500)
            throw new DomainException("A observação deve ter até 500 caracteres.");

        var grouped = request.Items.GroupBy(x => x.ProductId).Select(g => new CheckoutItemRequest(
            g.Key, g.Sum(x => x.Quantity), g.First().ConfirmedUnitPrice)).ToArray();
        if (grouped.Any(x => x.Quantity <= 0)) throw new DomainException("Todas as quantidades devem ser positivas.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var previous = await db.Orders.Include(x => x.Items).Include(x => x.Client)
            .SingleOrDefaultAsync(x => x.ClientId == clientId && x.IdempotencyKey == request.IdempotencyKey, ct);
        if (previous is not null) return previous;

        var orderedItems = grouped.OrderBy(x => x.ProductId).ToArray();
        var products = new List<Product>();
        foreach (var item in orderedItems)
        {
            var product = await db.Products
                .FromSqlInterpolated($"SELECT * FROM `products` WHERE `Id` = {item.ProductId} FOR UPDATE")
                .Include(x => x.Category).SingleOrDefaultAsync(ct)
                ?? throw new DomainException("Um produto do carrinho não existe.");
            if (!product.IsActive || !product.Category.IsActive)
                throw new DomainException($"O produto '{product.Name}' não está disponível.");
            if (item.ConfirmedUnitPrice != product.Price)
                throw new PriceChangedException(product.Id, product.Name, item.ConfirmedUnitPrice, product.Price);
            if (product.StockAvailable < item.Quantity)
                throw new InsufficientStockException(product.Id, product.Name, item.Quantity, product.StockAvailable);
            products.Add(product);
        }

        var order = new Order
        {
            ClientId = clientId,
            Number = $"LF{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}",
            IdempotencyKey = request.IdempotencyKey,
            Observation = string.IsNullOrWhiteSpace(request.Observation) ? null : request.Observation.Trim(),
            Status = OrderStatus.Received
        };
        foreach (var pair in orderedItems.Zip(products))
        {
            var item = pair.First;
            var product = pair.Second;
            var before = product.StockAvailable;
            product.StockAvailable -= item.Quantity;
            product.Version++;
            var subtotal = OrderRules.CalculateSubtotal(product.Price, item.Quantity);
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id, SkuSnapshot = product.Sku, ProductNameSnapshot = product.Name,
                Quantity = item.Quantity, UnitPrice = product.Price, Subtotal = subtotal
            });
            db.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id, Order = order, Quantity = -item.Quantity,
                PreviousBalance = before, NewBalance = product.StockAvailable,
                Reason = "Reserva de estoque ao receber pedido", AuthorId = clientId
            });
        }
        order.Total = order.Items.Sum(x => x.Subtotal);
        order.History.Add(new OrderHistory { NewStatus = OrderStatus.Received, AuthorId = clientId, Note = "Pedido realizado pelo cliente" });
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        order.Client = await db.Users.FindAsync([clientId], ct) ?? null!;
        return order;
    }

    public async Task<Order> CancelAsync(Guid orderId, Guid actorId, bool actorIsEmployee, long expectedVersion, string? note, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.Orders.FromSqlInterpolated($"SELECT * FROM `orders` WHERE `Id` = {orderId} FOR UPDATE")
            .SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Pedido não encontrado.");
        if (!actorIsEmployee && order.ClientId != actorId) throw new UnauthorizedAccessException("Este pedido pertence a outro cliente.");
        if (order.Version != expectedVersion) throw new ConcurrencyException();
        var allowed = actorIsEmployee ? OrderRules.CanEmployeeCancel(order.Status) : OrderRules.CanClientCancel(order.Status);
        if (!allowed) throw new DomainException("O pedido não pode mais ser cancelado.");

        var previous = order.Status;
        var items = await db.OrderItems.Where(x => x.OrderId == order.Id).OrderBy(x => x.ProductId).ToListAsync(ct);
        foreach (var item in items)
        {
            var product = await db.Products
                .FromSqlInterpolated($"SELECT * FROM `products` WHERE `Id` = {item.ProductId} FOR UPDATE")
                .SingleAsync(ct);
            var before = product.StockAvailable;
            product.StockAvailable += item.Quantity;
            product.Version++;
            db.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id, OrderId = order.Id, Quantity = item.Quantity,
                PreviousBalance = before, NewBalance = product.StockAvailable,
                Reason = "Devolução por cancelamento", AuthorId = actorId
            });
        }
        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = order.UpdatedAt = DateTimeOffset.UtcNow;
        order.Version++;
        db.OrderHistory.Add(new OrderHistory { OrderId = order.Id, PreviousStatus = previous, NewStatus = OrderStatus.Cancelled, AuthorId = actorId, Note = note });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return order;
    }

    public async Task<Order> ChangeStatusAsync(Guid orderId, Guid actorId, UpdateOrderStatusRequest request, CancellationToken ct)
    {
        if (request.NewStatus == OrderStatus.Completed && !request.ConfirmPayment)
            throw new DomainException("Confirme o pagamento presencial para concluir a retirada.");
        if (request.NewStatus == OrderStatus.Cancelled)
            return await CancelAsync(orderId, actorId, true, request.ExpectedVersion, request.Note, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.Orders.FromSqlInterpolated($"SELECT * FROM `orders` WHERE `Id` = {orderId} FOR UPDATE")
            .SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Pedido não encontrado.");
        if (order.Version != request.ExpectedVersion) throw new ConcurrencyException();
        OrderRules.EnsureTransition(order.Status, request.NewStatus);
        var previous = order.Status;
        order.Status = request.NewStatus;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.Version++;
        if (request.NewStatus == OrderStatus.Completed)
        {
            order.CompletedAt = order.UpdatedAt;
            order.PaymentConfirmed = true;
            order.PaymentConfirmedAt = order.UpdatedAt;
            order.PaymentConfirmedById = actorId;
        }
        db.OrderHistory.Add(new OrderHistory { OrderId = order.Id, PreviousStatus = previous, NewStatus = order.Status, AuthorId = actorId, Note = request.Note });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return order;
    }
}

public sealed class PriceChangedException(Guid productId, string productName, decimal confirmedPrice, decimal currentPrice)
    : Exception($"O preço de '{productName}' mudou de {confirmedPrice:C} para {currentPrice:C}.")
{
    public Guid ProductId { get; } = productId;
    public decimal ConfirmedPrice { get; } = confirmedPrice;
    public decimal CurrentPrice { get; } = currentPrice;
}
public sealed class InsufficientStockException(Guid productId, string productName, int requested, int available)
    : Exception($"Estoque insuficiente para '{productName}': solicitado {requested}, disponível {available}.")
{
    public Guid ProductId { get; } = productId;
}
public sealed class ConcurrencyException() : Exception("Os dados foram alterados por outra operação. Atualize a tela e tente novamente.");

using LimpaFacil.Api.Services;
using LimpaFacil.Contracts;
using LimpaFacil.Domain;
using Microsoft.EntityFrameworkCore;

namespace LimpaFacil.IntegrationTests;

public sealed class OrderMySqlTests
{
    [MySqlFact]
    public async Task Checkout_is_idempotent_and_cancel_restores_stock_once()
    {
        await using var server = await MySqlTestDatabase.CreateAsync(); await using var db = server.CreateContext();
        var seed = await Seed(db, 3); var service = new OrderService(db); var request = new CheckoutRequest("logical-attempt-1", [new(seed.Product.Id, 2, 10m)]);
        var first = await service.CheckoutAsync(seed.Client.Id, request, default); var retry = await service.CheckoutAsync(seed.Client.Id, request, default);
        Assert.Equal(first.Id, retry.Id); Assert.Equal(1, (await db.Products.FindAsync(seed.Product.Id))!.StockAvailable);
        await service.CancelAsync(first.Id, seed.Client.Id, false, first.Version, null, default);
        Assert.Equal(3, (await db.Products.FindAsync(seed.Product.Id))!.StockAvailable);
        await Assert.ThrowsAsync<DomainException>(() => service.CancelAsync(first.Id, seed.Client.Id, false, first.Version, null, default));
        Assert.Single(await db.Orders.ToListAsync());
        Assert.Equal(0, await db.StockMovements.SumAsync(x => x.Quantity));
    }

    [MySqlFact]
    public async Task Two_concurrent_purchases_cannot_sell_the_last_unit_twice()
    {
        await using var server = await MySqlTestDatabase.CreateAsync(); Guid productId; Guid a; Guid b;
        await using (var seedDb = server.CreateContext()) { var seed = await Seed(seedDb, 1); productId = seed.Product.Id; a = seed.Client.Id; var other = new User { Name = "B", Email = "b@test.local", PasswordHash = "x" }; seedDb.Add(other); await seedDb.SaveChangesAsync(); b = other.Id; }
        async Task<bool> Buy(Guid client, string key) { try { await using var db = server.CreateContext(); await new OrderService(db).CheckoutAsync(client, new(key, [new(productId, 1, 10m)]), default); return true; } catch (Exception ex) when (ex is InsufficientStockException or DbUpdateException) { return false; } }
        var result = await Task.WhenAll(Buy(a, "a"), Buy(b, "b")); Assert.Single(result.Where(x => x));
    }

    [MySqlFact]
    public async Task Insufficient_stock_and_price_changes_do_not_reserve_products()
    {
        await using var server = await MySqlTestDatabase.CreateAsync(); await using var db = server.CreateContext();
        var seed = await Seed(db, 1); var service = new OrderService(db);
        await Assert.ThrowsAsync<InsufficientStockException>(() => service.CheckoutAsync(seed.Client.Id, new("too-many", [new(seed.Product.Id, 2, 10m)]), default));
        await Assert.ThrowsAsync<PriceChangedException>(() => service.CheckoutAsync(seed.Client.Id, new("old-price", [new(seed.Product.Id, 1, 9m)]), default));
        Assert.Equal(1, (await db.Products.FindAsync(seed.Product.Id))!.StockAvailable);
        Assert.Empty(await db.Orders.ToListAsync());
    }

    [MySqlFact]
    public async Task Completing_pickup_requires_payment_and_preserves_item_snapshot()
    {
        await using var server = await MySqlTestDatabase.CreateAsync(); await using var db = server.CreateContext();
        var seed = await Seed(db, 3); var service = new OrderService(db);
        var order = await service.CheckoutAsync(seed.Client.Id, new("pickup", [new(seed.Product.Id, 2, 10m)], "  Separar  "), default);
        Assert.Equal("Separar", order.Observation);
        seed.Product.Name = "Novo nome"; seed.Product.Price = 20m; await db.SaveChangesAsync();
        Assert.Equal("Produto", order.Items.Single().ProductNameSnapshot);
        Assert.Equal(10m, order.Items.Single().UnitPrice);
        await service.ChangeStatusAsync(order.Id, seed.Client.Id, new(OrderStatus.InSeparation, order.Version, null), default);
        await service.ChangeStatusAsync(order.Id, seed.Client.Id, new(OrderStatus.ReadyForPickup, order.Version, null), default);
        await Assert.ThrowsAsync<DomainException>(() => service.ChangeStatusAsync(order.Id, seed.Client.Id, new(OrderStatus.Completed, order.Version, null), default));
        await service.ChangeStatusAsync(order.Id, seed.Client.Id, new(OrderStatus.Completed, order.Version, null, true), default);
        Assert.True(order.PaymentConfirmed);
        Assert.Equal(seed.Client.Id, order.PaymentConfirmedById);
        Assert.Equal(20m, order.Total);
        Assert.Equal(4, order.History.Count);
        await Assert.ThrowsAsync<DomainException>(() => service.CancelAsync(order.Id, seed.Client.Id, true, order.Version, null, default));
    }

    [MySqlFact]
    public async Task Inactive_categories_cannot_receive_orders()
    {
        await using var server = await MySqlTestDatabase.CreateAsync(); await using var db = server.CreateContext();
        var seed = await Seed(db, 3); seed.Product.Category.IsActive = false; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<DomainException>(() => new OrderService(db).CheckoutAsync(seed.Client.Id, new("inactive", [new(seed.Product.Id, 1, 10m)]), default));
        Assert.Empty(await db.Orders.ToListAsync());
    }

    private static async Task<(User Client, Product Product)> Seed(LimpaFacil.Api.Data.AppDbContext db, int stock)
    {
        var client = new User { Name = "Cliente", Email = $"{Guid.NewGuid():N}@test.local", PasswordHash = "x" };
        var category = new Category { Name = $"Cat-{Guid.NewGuid():N}" }; var product = new Product { Category = category, Sku = $"SKU-{Guid.NewGuid():N}", Name = "Produto", Brand = "Marca", Presentation = "1 un", Price = 10m, StockAvailable = stock };
        db.AddRange(client, category, product); await db.SaveChangesAsync(); return (client, product);
    }
}

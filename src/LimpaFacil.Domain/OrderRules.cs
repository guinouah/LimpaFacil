namespace LimpaFacil.Domain;

public static class OrderRules
{
    public static decimal CalculateSubtotal(decimal unitPrice, int quantity)
    {
        if (unitPrice <= 0) throw new DomainException("O preço deve ser maior que zero.");
        if (quantity <= 0) throw new DomainException("A quantidade deve ser inteira e positiva.");
        return decimal.Round(unitPrice * quantity, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateTotal(IEnumerable<(decimal UnitPrice, int Quantity)> items)
    {
        var materialized = items.ToArray();
        if (materialized.Length == 0) throw new DomainException("O pedido deve ter pelo menos um item.");
        return materialized.Sum(x => CalculateSubtotal(x.UnitPrice, x.Quantity));
    }

    public static bool CanClientCancel(OrderStatus status) => status == OrderStatus.Received;

    public static bool CanEmployeeCancel(OrderStatus status) =>
        status is OrderStatus.Received or OrderStatus.InSeparation;

    public static bool CanTransition(OrderStatus current, OrderStatus next) => (current, next) switch
    {
        (OrderStatus.Received, OrderStatus.InSeparation) => true,
        (OrderStatus.InSeparation, OrderStatus.ReadyForPickup) => true,
        (OrderStatus.ReadyForPickup, OrderStatus.Completed) => true,
        (OrderStatus.Received, OrderStatus.Cancelled) => true,
        (OrderStatus.InSeparation, OrderStatus.Cancelled) => true,
        _ => false
    };

    public static void EnsureTransition(OrderStatus current, OrderStatus next)
    {
        if (!CanTransition(current, next))
            throw new DomainException($"Transição inválida: {current.ToPtBr()} → {next.ToPtBr()}.");
    }
}

public sealed class DomainException(string message) : Exception(message);

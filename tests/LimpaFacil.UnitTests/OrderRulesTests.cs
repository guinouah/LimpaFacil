using LimpaFacil.Contracts;
using LimpaFacil.Domain;

namespace LimpaFacil.UnitTests;

public sealed class OrderRulesTests
{
    [Fact]
    public void Calculates_total_with_decimal_money() =>
        Assert.Equal(25.55m, OrderRules.CalculateTotal([(3.55m, 3), (7.45m, 2)]));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_non_positive_quantity(int quantity) =>
        Assert.Throws<DomainException>(() => OrderRules.CalculateSubtotal(10m, quantity));

    [Fact]
    public void Enforces_order_state_machine()
    {
        Assert.True(OrderRules.CanTransition(OrderStatus.Received, OrderStatus.InSeparation));
        Assert.True(OrderRules.CanTransition(OrderStatus.ReadyForPickup, OrderStatus.Completed));
        Assert.False(OrderRules.CanTransition(OrderStatus.Received, OrderStatus.Completed));
        Assert.False(OrderRules.CanTransition(OrderStatus.Completed, OrderStatus.Cancelled));
    }

    [Fact]
    public void Public_registration_contract_cannot_assign_privileges() =>
        Assert.DoesNotContain(typeof(RegisterRequest).GetProperties(), x => x.Name.Contains("Role", StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void Historical_item_keeps_snapshot_after_product_changes()
    {
        var product = new Product { Sku = "A", Name = "Original", Price = 5m };
        var item = new OrderItem { SkuSnapshot = product.Sku, ProductNameSnapshot = product.Name, UnitPrice = product.Price };
        product.Name = "Novo"; product.Price = 9m;
        Assert.Equal("Original", item.ProductNameSnapshot); Assert.Equal(5m, item.UnitPrice);
    }
}

using LimpaFacil.Contracts;
using LimpaFacil.Mobile.Services;

namespace LimpaFacil.UnitTests;

public sealed class LocalStoreTests : IAsyncLifetime
{
    private readonly string path = Path.Combine(AppContext.BaseDirectory, $"cart-{Guid.NewGuid():N}.db3");
    private readonly List<LocalStore> stores = [];
    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() { foreach (var store in stores) await store.DisposeAsync(); if (File.Exists(path)) File.Delete(path); }
    private LocalStore Store() { var store = new LocalStore(path); stores.Add(store); return store; }

    [Fact]
    public async Task Cart_persists_and_is_isolated_per_user()
    {
        var first = Store(); var a = Guid.NewGuid(); var b = Guid.NewGuid(); var p = Product();
        await first.AddCartAsync(a, p); await first.AddCartAsync(a, p); await first.AddCartAsync(b, p);
        var reopened = Store();
        Assert.Equal(2, Assert.Single(await reopened.GetCartAsync(a)).Quantity);
        Assert.Equal(1, Assert.Single(await reopened.GetCartAsync(b)).Quantity);
        await reopened.ClearPrivateAsync(a);
        Assert.Empty(await reopened.GetCartAsync(a)); Assert.Single(await reopened.GetCartAsync(b));
    }

    [Fact]
    public async Task Checkout_key_is_reused_after_network_failure_but_changes_with_cart()
    {
        var store = Store(); var user = Guid.NewGuid(); await store.AddCartAsync(user, Product());
        var cart = await store.GetCartAsync(user); var first = await store.GetCheckoutKeyAsync(user, cart); var retry = await store.GetCheckoutKeyAsync(user, cart);
        Assert.Equal(first, retry);
        await store.UpdateQuantityAsync(cart[0], 2); var changed = await store.GetCheckoutKeyAsync(user, await store.GetCartAsync(user));
        Assert.NotEqual(first, changed);
    }

    [Fact]
    public async Task Catalog_cache_returns_products_and_update_date_offline()
    {
        var store = Store(); await store.SaveCatalogAsync([Product()]);
        var cache = await Store().GetCatalogAsync();
        Assert.Single(cache.Products); Assert.NotNull(cache.UpdatedAt);
    }

    [Fact]
    public async Task Repeating_order_replaces_cart_and_skips_unavailable_products()
    {
        var store = Store(); var user = Guid.NewGuid(); var available = Product();
        await store.AddCartAsync(user, Product());
        var unavailableId = Guid.NewGuid();
        var items = new[]
        {
            new OrderItemDto(available.Id, available.Sku, available.Name, 3, available.Price, available.Price * 3),
            new OrderItemDto(unavailableId, "OLD", "Produto inativo", 1, 1m, 1m)
        };

        var result = await store.ReplaceCartFromOrderAsync(user, items, [available]);

        var row = Assert.Single(await store.GetCartAsync(user));
        Assert.Equal(3, row.Quantity);
        Assert.Equal(1, result.Added);
        Assert.Equal(1, result.Skipped);
    }

    private static ProductDto Product() => new(Guid.NewGuid(), Guid.NewGuid(), "Limpeza", "SKU-1", "Detergente", null, "Marca", "500 ml", null, 4.50m, 10, null, true, 1);
}

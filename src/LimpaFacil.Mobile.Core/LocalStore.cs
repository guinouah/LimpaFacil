using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LimpaFacil.Contracts;
using SQLite;

namespace LimpaFacil.Mobile.Services;

public sealed class LocalStore : IAsyncDisposable
{
    private readonly string databasePath;
    private SQLiteAsyncConnection? db;
    private readonly SemaphoreSlim initialization = new(1, 1);
    public LocalStore() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "limpafacil.db3")) { }
    public LocalStore(string databasePath) => this.databasePath = databasePath;
    private async Task<SQLiteAsyncConnection> Db()
    {
        if (db is not null) return db;
        await initialization.WaitAsync();
        try
        {
            if (db is not null) return db;
            var connection = new SQLiteAsyncConnection(databasePath);
            await connection.CreateTableAsync<CachedProduct>(); await connection.CreateTableAsync<CartRow>(); await connection.CreateTableAsync<CheckoutAttempt>();
            db = connection;
            return connection;
        }
        finally { initialization.Release(); }
    }
    public async Task SaveCatalogAsync(IEnumerable<ProductDto> products)
    {
        var conn = await Db(); var now = DateTimeOffset.UtcNow;
        var rows = products.Select(x => new CachedProduct { Id = x.Id.ToString(), Json = JsonSerializer.Serialize(x), UpdatedAtUtc = now.UtcDateTime }).ToArray();
        await conn.RunInTransactionAsync(transaction => { transaction.DeleteAll<CachedProduct>(); transaction.InsertAll(rows); });
    }
    public async Task<(List<ProductDto> Products, DateTimeOffset? UpdatedAt)> GetCatalogAsync()
    {
        var rows = await (await Db()).Table<CachedProduct>().ToListAsync();
        return (rows.Select(x => JsonSerializer.Deserialize<ProductDto>(x.Json)!).ToList(), rows.Count == 0 ? null : new DateTimeOffset(rows.Max(x => x.UpdatedAtUtc), TimeSpan.Zero));
    }
    public async Task<List<CartRow>> GetCartAsync(Guid userId)
    {
        var value = userId.ToString();
        return await (await Db()).Table<CartRow>().Where(x => x.UserId == value).ToListAsync();
    }
    public async Task AddCartAsync(Guid userId, ProductDto p, int quantity = 1)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        var conn = await Db(); var key = $"{userId}:{p.Id}"; var row = await conn.FindAsync<CartRow>(key);
        if (row is null) await conn.InsertAsync(new CartRow { Key = key, UserId = userId.ToString(), ProductId = p.Id.ToString(), Name = p.Name, Sku = p.Sku, UnitPrice = p.Price, Quantity = quantity });
        else { row.Quantity += quantity; row.UnitPrice = p.Price; await conn.UpdateAsync(row); }
        await conn.ExecuteAsync("DELETE FROM checkout_attempts WHERE UserId = ?", userId.ToString());
    }
    public async Task<(int Added, int Skipped)> ReplaceCartFromOrderAsync(Guid userId, IEnumerable<OrderItemDto> items, IEnumerable<ProductDto> availableProducts)
    {
        var products = availableProducts.Where(x => x.IsActive).ToDictionary(x => x.Id);
        await ClearCartAsync(userId);
        var added = 0; var skipped = 0;
        foreach (var item in items)
        {
            if (!products.TryGetValue(item.ProductId, out var product)) { skipped++; continue; }
            await AddCartAsync(userId, product, item.Quantity); added++;
        }
        return (added, skipped);
    }
    public async Task UpdateQuantityAsync(CartRow row, int quantity)
    {
        var conn = await Db(); if (quantity <= 0) await conn.DeleteAsync(row); else { row.Quantity = quantity; await conn.UpdateAsync(row); }
        await conn.ExecuteAsync("DELETE FROM checkout_attempts WHERE UserId = ?", row.UserId);
    }
    public async Task<string> GetCheckoutKeyAsync(Guid userId, IEnumerable<CartRow> cart)
    {
        var conn = await Db(); var userValue = userId.ToString(); var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', cart.OrderBy(x => x.ProductId).Select(x => $"{x.ProductId}:{x.Quantity}:{x.UnitPrice}")))));
        var old = await conn.Table<CheckoutAttempt>().Where(x => x.UserId == userValue).FirstOrDefaultAsync();
        if (old?.Fingerprint == fingerprint) return old.IdempotencyKey;
        if (old is not null) await conn.DeleteAsync(old);
        var attempt = new CheckoutAttempt { UserId = userValue, Fingerprint = fingerprint, IdempotencyKey = Guid.NewGuid().ToString("N") };
        await conn.InsertAsync(attempt); return attempt.IdempotencyKey;
    }
    public async Task ClearCartAsync(Guid userId) { var conn = await Db(); await conn.ExecuteAsync("DELETE FROM cart_rows WHERE UserId = ?", userId.ToString()); await conn.ExecuteAsync("DELETE FROM checkout_attempts WHERE UserId = ?", userId.ToString()); }
    public Task ClearPrivateAsync(Guid userId) => ClearCartAsync(userId);
    public async ValueTask DisposeAsync()
    {
        if (db is not null) { await db.CloseAsync(); db = null; }
    }
}

[Table("cached_products")]
public sealed class CachedProduct { [PrimaryKey] public string Id { get; set; } = ""; public string Json { get; set; } = ""; public DateTime UpdatedAtUtc { get; set; } }
[Table("cart_rows")]
public sealed class CartRow { [PrimaryKey] public string Key { get; set; } = ""; [Indexed] public string UserId { get; set; } = ""; public string ProductId { get; set; } = ""; public string Sku { get; set; } = ""; public string Name { get; set; } = ""; public decimal UnitPrice { get; set; } public int Quantity { get; set; } }
[Table("checkout_attempts")]
public sealed class CheckoutAttempt { [PrimaryKey, AutoIncrement] public int Id { get; set; } [Indexed] public string UserId { get; set; } = ""; public string Fingerprint { get; set; } = ""; public string IdempotencyKey { get; set; } = ""; }

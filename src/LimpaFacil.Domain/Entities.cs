using System.ComponentModel.DataAnnotations;

namespace LimpaFacil.Domain;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

public sealed class User : Entity
{
    [MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(180)] public string Email { get; set; } = string.Empty;
    public CustomerType? CustomerType { get; set; }
    [MaxLength(14)] public string? TaxId { get; set; }
    [MaxLength(500)] public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Client;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Order> Orders { get; set; } = [];
}

public sealed class Category : Entity
{
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Product> Products { get; set; } = [];
}

public sealed class Product : Entity
{
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    [MaxLength(50)] public string Sku { get; set; } = string.Empty;
    [MaxLength(140)] public string Name { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(100)] public string Brand { get; set; } = string.Empty;
    [MaxLength(100)] public string Presentation { get; set; } = string.Empty;
    [MaxLength(2000)] public string? UsageInstructions { get; set; }
    public decimal Price { get; set; }
    public int StockAvailable { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    [ConcurrencyCheck] public long Version { get; set; } = 1;
}

public sealed class Order : Entity
{
    [MaxLength(30)] public string Number { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public User Client { get; set; } = null!;
    public OrderStatus Status { get; set; } = OrderStatus.Received;
    public decimal Total { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    [MaxLength(80)] public string IdempotencyKey { get; set; } = string.Empty;
    [MaxLength(500)] public string? Observation { get; set; }
    [ConcurrencyCheck] public long Version { get; set; } = 1;
    public bool PaymentConfirmed { get; set; }
    public DateTimeOffset? PaymentConfirmedAt { get; set; }
    public Guid? PaymentConfirmedById { get; set; }
    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<OrderHistory> History { get; set; } = [];
}

public sealed class OrderItem : Entity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    [MaxLength(50)] public string SkuSnapshot { get; set; } = string.Empty;
    [MaxLength(140)] public string ProductNameSnapshot { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

public sealed class OrderHistory : Entity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public OrderStatus? PreviousStatus { get; set; }
    public OrderStatus NewStatus { get; set; }
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    [MaxLength(500)] public string? Note { get; set; }
}

public sealed class StockMovement : Entity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    public int Quantity { get; set; }
    public int PreviousBalance { get; set; }
    public int NewBalance { get; set; }
    [MaxLength(300)] public string Reason { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class RefreshSession : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    [MaxLength(128)] public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
    [MaxLength(200)] public string? Device { get; set; }
}

using LimpaFacil.Domain;
using Microsoft.EntityFrameworkCore;

namespace LimpaFacil.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderHistory> OrderHistory => Set<OrderHistory>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.TaxId).IsUnique();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.CustomerType).HasConversion<string>().HasMaxLength(20);
        });
        b.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasIndex(x => x.Name).IsUnique();
        });
        b.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasIndex(x => x.Sku).IsUnique();
            e.Property(x => x.Price).HasPrecision(12, 2);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_products_price", "`Price` > 0"));
            e.ToTable(t => t.HasCheckConstraint("CK_products_stock", "`StockAvailable` >= 0"));
        });
        b.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasIndex(x => x.Number).IsUnique();
            e.HasIndex(x => new { x.ClientId, x.IdempotencyKey }).IsUnique();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Total).HasPrecision(12, 2);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne(x => x.Client).WithMany(x => x.Orders).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<OrderItem>(e =>
        {
            e.ToTable("order_items");
            e.Property(x => x.UnitPrice).HasPrecision(12, 2);
            e.Property(x => x.Subtotal).HasPrecision(12, 2);
            e.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_order_items_quantity", "`Quantity` > 0"));
        });
        b.Entity<OrderHistory>(e =>
        {
            e.ToTable("order_history");
            e.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(30);
            e.HasOne(x => x.Order).WithMany(x => x.History).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<StockMovement>(e =>
        {
            e.ToTable("stock_movements");
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<RefreshSession>(e =>
        {
            e.ToTable("refresh_sessions");
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

using LimpaFacil.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LimpaFacil.Api.Data;

public sealed class DatabaseSeeder(AppDbContext db, IPasswordHasher<User> hasher)
{
    public async Task SeedDemoAsync(CancellationToken ct = default)
    {
        if (await db.Categories.AnyAsync(ct)) return;

        var cleaning = new Category { Name = "Limpeza geral", Description = "Produtos para a rotina da casa" };
        var laundry = new Category { Name = "Lavanderia", Description = "Cuidados para roupas" };
        db.AddRange(cleaning, laundry);
        db.Products.AddRange(
            new Product { Category = cleaning, Sku = "DET-NEU-500", Name = "Detergente neutro", Brand = "LimpaFácil", Presentation = "Frasco 500 ml", UsageInstructions = "Aplique em esponja úmida.", Price = 3.90m, StockAvailable = 50 },
            new Product { Category = cleaning, Sku = "DES-LAV-2L", Name = "Desinfetante lavanda", Brand = "LimpaFácil", Presentation = "Garrafa 2 L", UsageInstructions = "Dilua conforme o rótulo.", Price = 9.50m, StockAvailable = 30 },
            new Product { Category = laundry, Sku = "SAB-LIQ-3L", Name = "Sabão líquido", Brand = "LimpaFácil", Presentation = "Galão 3 L", UsageInstructions = "Use a tampa como dosador.", Price = 24.90m, StockAvailable = 20 });
        await db.SaveChangesAsync(ct);
    }

    public async Task CreateFirstAdminFromEnvironmentAsync(CancellationToken ct = default)
    {
        var email = Environment.GetEnvironmentVariable("LIMPAFACIL_ADMIN_EMAIL")?.Trim().ToLowerInvariant();
        var password = Environment.GetEnvironmentVariable("LIMPAFACIL_ADMIN_PASSWORD");
        var name = Environment.GetEnvironmentVariable("LIMPAFACIL_ADMIN_NAME")?.Trim() ?? "Administrador";
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Defina LIMPAFACIL_ADMIN_EMAIL e LIMPAFACIL_ADMIN_PASSWORD.");
        if (password.Length < 10) throw new InvalidOperationException("A senha inicial deve ter ao menos 10 caracteres.");
        if (await db.Users.AnyAsync(x => x.Role == UserRole.Administrator, ct))
            throw new InvalidOperationException("Já existe um administrador; a operação foi recusada.");
        var user = new User { Name = name, Email = email, Role = UserRole.Administrator };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }
}

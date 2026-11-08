using LimpaFacil.Api.Data;
using LimpaFacil.Api.Mapping;
using LimpaFacil.Api.Services;
using LimpaFacil.Contracts;
using LimpaFacil.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LimpaFacil.Api.Controllers;

[ApiController, Route("api/admin"), Authorize(Roles = "Employee,Administrator")]
public sealed class AdminController(AppDbContext db, CurrentUser current, OrderService orders, IPasswordHasher<User> hasher) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<IReadOnlyList<CategoryDto>> Categories(CancellationToken ct) =>
        await db.Categories.AsNoTracking().OrderBy(x => x.Name).Select(x => x.ToDto()).ToListAsync(ct);

    [HttpPost("categories")]
    public async Task<ActionResult<CategoryDto>> CreateCategory(SaveCategoryRequest request, CancellationToken ct)
    {
        var name = ValidateName(request.Name, 100);
        if (await db.Categories.AnyAsync(x => x.Name == name, ct)) return Conflict(new ApiError("duplicate_name", "Já existe uma categoria com este nome."));
        var entity = new Category { Name = name, Description = request.Description?.Trim(), IsActive = request.IsActive };
        db.Add(entity); await db.SaveChangesAsync(ct); return Created($"api/admin/categories/{entity.Id}", entity.ToDto());
    }

    [HttpPut("categories/{id:guid}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(Guid id, SaveCategoryRequest request, CancellationToken ct)
    {
        var entity = await db.Categories.FindAsync([id], ct); if (entity is null) return NotFound();
        var name = ValidateName(request.Name, 100);
        if (await db.Categories.AnyAsync(x => x.Id != id && x.Name == name, ct)) return Conflict(new ApiError("duplicate_name", "Já existe uma categoria com este nome."));
        entity.Name = name; entity.Description = request.Description?.Trim(); entity.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct); return Ok(entity.ToDto());
    }

    [HttpGet("products")]
    public async Task<IReadOnlyList<ProductDto>> Products(CancellationToken ct) =>
        await db.Products.AsNoTracking().Include(x => x.Category).OrderBy(x => x.Name).Select(x => x.ToDto()).ToListAsync(ct);

    [HttpPost("products")]
    public async Task<ActionResult<ProductDto>> CreateProduct(SaveProductRequest request, CancellationToken ct)
    {
        ValidateProduct(request);
        if (!await db.Categories.AnyAsync(x => x.Id == request.CategoryId, ct)) return ValidationProblem("Categoria inexistente.");
        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await db.Products.AnyAsync(x => x.Sku == sku, ct)) return Conflict(new ApiError("duplicate_sku", "O SKU já está cadastrado."));
        var entity = Apply(new Product(), request); entity.Sku = sku;
        db.Add(entity); await db.SaveChangesAsync(ct); await db.Entry(entity).Reference(x => x.Category).LoadAsync(ct);
        return Created($"api/admin/products/{entity.Id}", entity.ToDto());
    }

    [HttpPut("products/{id:guid}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, SaveProductRequest request, CancellationToken ct)
    {
        ValidateProduct(request);
        var entity = await db.Products.Include(x => x.Category).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (request.ExpectedVersion != entity.Version) throw new ConcurrencyException();
        if (!await db.Categories.AnyAsync(x => x.Id == request.CategoryId, ct)) return ValidationProblem("Categoria inexistente.");
        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await db.Products.AnyAsync(x => x.Id != id && x.Sku == sku, ct)) return Conflict(new ApiError("duplicate_sku", "O SKU já está cadastrado."));
        Apply(entity, request); entity.Sku = sku; entity.Version++;
        await db.SaveChangesAsync(ct); await db.Entry(entity).Reference(x => x.Category).LoadAsync(ct);
        return Ok(entity.ToDto());
    }

    [HttpPost("products/{id:guid}/stock-adjustments"), Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ProductDto>> AdjustStock(Guid id, StockAdjustmentRequest request, CancellationToken ct)
    {
        if (request.Quantity == 0 || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 300) return ValidationProblem("Informe quantidade diferente de zero e motivo com até 300 caracteres.");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var product = await db.Products.FromSqlInterpolated($"SELECT * FROM `products` WHERE `Id` = {id} FOR UPDATE").Include(x => x.Category).SingleOrDefaultAsync(ct);
        if (product is null) return NotFound();
        if (product.Version != request.ExpectedProductVersion) throw new ConcurrencyException();
        var nextValue = (long)product.StockAvailable + request.Quantity;
        if (nextValue < 0 || nextValue > int.MaxValue) throw new DomainException("O ajuste deixaria o estoque fora do limite permitido.");
        var next = (int)nextValue;
        var before = product.StockAvailable; product.StockAvailable = next; product.Version++;
        db.StockMovements.Add(new StockMovement { ProductId = id, Quantity = request.Quantity, PreviousBalance = before,
            NewBalance = next, Reason = request.Reason.Trim(), AuthorId = current.Id });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Ok(product.ToDto());
    }

    [HttpGet("orders")]
    public async Task<PagedResponse<OrderDto>> Orders([FromQuery] OrderStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Orders.AsNoTracking().Include(x => x.Client).Include(x => x.Items).Include(x => x.History).ThenInclude(x => x.Author).AsSplitQuery();
        if (status.HasValue) query = query.Where(x => x.Status == status);
        var total = await query.CountAsync(ct);
        var list = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(list.Select(x => x.ToDto()).ToArray(), total, page, pageSize);
    }

    [HttpPut("orders/{id:guid}/status")]
    public async Task<ActionResult<OrderDto>> ChangeStatus(Guid id, UpdateOrderStatusRequest request, CancellationToken ct)
    {
        await orders.ChangeStatusAsync(id, current.Id, request, ct);
        var entity = await db.Orders.AsNoTracking().Include(x => x.Client).Include(x => x.Items)
            .Include(x => x.History).ThenInclude(x => x.Author).AsSplitQuery().SingleAsync(x => x.Id == id, ct);
        return Ok(entity.ToDto());
    }

    [HttpGet("employees"), Authorize(Roles = "Administrator")]
    public async Task<IReadOnlyList<EmployeeDto>> Employees(CancellationToken ct) =>
        await db.Users.AsNoTracking().Where(x => x.Role != UserRole.Client).OrderBy(x => x.Name)
            .Select(x => new EmployeeDto(x.Id, x.Name, x.Email, x.Role, x.IsActive)).ToListAsync(ct);

    [HttpPost("employees"), Authorize(Roles = "Administrator")]
    public async Task<ActionResult<UserSummary>> CreateEmployee(CreateEmployeeRequest request, CancellationToken ct)
    {
        if (request.Role is not (UserRole.Employee or UserRole.Administrator)) return ValidationProblem("Perfil inválido para funcionário.");
        if (request.Password.Length < 10) return ValidationProblem("A senha deve ter ao menos 10 caracteres.");
        var email = request.Email.Trim().ToLowerInvariant();
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email)) return ValidationProblem("E-mail inválido.");
        if (await db.Users.AnyAsync(x => x.Email == email, ct)) return Conflict(new ApiError("email_in_use", "E-mail já cadastrado."));
        var user = new User { Name = ValidateName(request.Name, 120), Email = email, Role = request.Role };
        user.PasswordHash = hasher.HashPassword(user, request.Password); db.Users.Add(user); await db.SaveChangesAsync(ct);
        return Created($"api/admin/employees/{user.Id}", new UserSummary(user.Id, user.Name, user.Email, user.Role));
    }

    [HttpPut("employees/{id:guid}/active"), Authorize(Roles = "Administrator")]
    public async Task<IActionResult> SetEmployeeActive(Guid id, SetUserActiveRequest request, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct); if (user is null || user.Role == UserRole.Client) return NotFound();
        if (id == current.Id && !request.IsActive) return ValidationProblem("Você não pode desativar a própria conta.");
        user.IsActive = request.IsActive; await db.SaveChangesAsync(ct); return NoContent();
    }

    private static string ValidateName(string value, int max)
    {
        var text = value.Trim(); if (text.Length is < 2 || text.Length > max) throw new DomainException($"O nome deve ter entre 2 e {max} caracteres."); return text;
    }
    private static void ValidateProduct(SaveProductRequest r)
    {
        if (r.Price <= 0 || r.Price > 9999999999.99m) throw new DomainException("O preço deve ser maior que zero e caber em 12 dígitos com 2 casas decimais.");
        if (string.IsNullOrWhiteSpace(r.Sku) || string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.Brand) || string.IsNullOrWhiteSpace(r.Presentation))
            throw new DomainException("SKU, nome, marca e apresentação são obrigatórios.");
        if (r.Sku.Trim().Length > 50 || r.Name.Trim().Length > 140 || r.Brand.Trim().Length > 100 || r.Presentation.Trim().Length > 100
            || r.Description?.Length > 1000 || r.UsageInstructions?.Length > 2000 || r.ImageUrl?.Length > 500)
            throw new DomainException("Um campo do produto ultrapassa o tamanho permitido.");
    }
    private static Product Apply(Product p, SaveProductRequest r)
    {
        p.CategoryId = r.CategoryId; p.Name = r.Name.Trim(); p.Description = r.Description?.Trim(); p.Brand = r.Brand.Trim();
        p.Presentation = r.Presentation.Trim(); p.UsageInstructions = r.UsageInstructions?.Trim(); p.Price = decimal.Round(r.Price, 2);
        p.ImageUrl = r.ImageUrl?.Trim(); p.IsActive = r.IsActive; return p;
    }
}

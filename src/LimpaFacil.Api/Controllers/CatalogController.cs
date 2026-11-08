using LimpaFacil.Api.Data;
using LimpaFacil.Api.Mapping;
using LimpaFacil.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LimpaFacil.Api.Controllers;

[ApiController, Route("api/catalog"), AllowAnonymous]
public sealed class CatalogController(AppDbContext db) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<IReadOnlyList<CategoryDto>> Categories(CancellationToken ct) =>
        await db.Categories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => x.ToDto()).ToListAsync(ct);

    [HttpGet("products")]
    public async Task<PagedResponse<ProductDto>> Products([FromQuery] string? search, [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Products.AsNoTracking().Include(x => x.Category)
            .Where(x => x.IsActive && x.Category.IsActive);
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term) || x.Brand.Contains(term) || x.Sku.Contains(term));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).Select(x => x.ToDto()).ToListAsync(ct);
        return new(items, total, page, pageSize);
    }

    [HttpGet("products/{id:guid}")]
    public async Task<ActionResult<ProductDto>> Product(Guid id, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().Include(x => x.Category)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive && x.Category.IsActive, ct);
        return product is null ? NotFound() : Ok(product.ToDto());
    }
}

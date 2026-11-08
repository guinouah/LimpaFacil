using LimpaFacil.Api.Data;
using LimpaFacil.Api.Mapping;
using LimpaFacil.Api.Services;
using LimpaFacil.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LimpaFacil.Api.Controllers;

[ApiController, Route("api/orders"), Authorize(Roles = "Client")]
public sealed class OrdersController(AppDbContext db, CurrentUser current, OrderService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Checkout(CheckoutRequest request, CancellationToken ct)
    {
        var order = await service.CheckoutAsync(current.Id, request, ct);
        var full = await Query().SingleAsync(x => x.Id == order.Id, ct);
        return Ok(full.ToDto());
    }

    [HttpGet]
    public async Task<IReadOnlyList<OrderDto>> Mine(CancellationToken ct) =>
        (await Query().Where(x => x.ClientId == current.Id).OrderByDescending(x => x.CreatedAt).ToListAsync(ct)).Select(x => x.ToDto()).ToArray();

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id, CancellationToken ct)
    {
        var order = await Query().SingleOrDefaultAsync(x => x.Id == id && x.ClientId == current.Id, ct);
        return order is null ? NotFound() : Ok(order.ToDto());
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(Guid id, CancelOrderRequest request, CancellationToken ct)
    {
        await service.CancelAsync(id, current.Id, false, request.ExpectedVersion, request.Note, ct);
        return Ok((await Query().SingleAsync(x => x.Id == id, ct)).ToDto());
    }

    private IQueryable<LimpaFacil.Domain.Order> Query() => db.Orders.AsNoTracking().Include(x => x.Client)
        .Include(x => x.Items).Include(x => x.History).ThenInclude(x => x.Author).AsSplitQuery();
}

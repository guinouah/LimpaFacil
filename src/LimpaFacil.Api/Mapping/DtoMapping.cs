using LimpaFacil.Contracts;
using LimpaFacil.Domain;

namespace LimpaFacil.Api.Mapping;

public static class DtoMapping
{
    public static CategoryDto ToDto(this Category x) => new(x.Id, x.Name, x.Description, x.IsActive);
    public static ProductDto ToDto(this Product x) => new(x.Id, x.CategoryId, x.Category.Name, x.Sku, x.Name,
        x.Description, x.Brand, x.Presentation, x.UsageInstructions, x.Price, x.StockAvailable, x.ImageUrl, x.IsActive, x.Version);
    public static OrderDto ToDto(this Order x) => new(x.Id, x.Number, x.ClientId, x.Client?.Name ?? string.Empty,
        x.Status, x.Total, x.CreatedAt, x.UpdatedAt, x.PaymentConfirmed, x.Observation, x.Version,
        x.Items.Select(i => new OrderItemDto(i.ProductId, i.SkuSnapshot, i.ProductNameSnapshot,
            i.Quantity, i.UnitPrice, i.Subtotal)).ToArray(),
        x.History.OrderBy(h => h.CreatedAt).Select(h => new OrderHistoryDto(h.PreviousStatus, h.NewStatus,
            h.Author?.Name ?? string.Empty, h.CreatedAt, h.Note)).ToArray());
}

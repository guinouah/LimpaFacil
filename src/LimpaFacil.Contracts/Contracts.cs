using LimpaFacil.Domain;

namespace LimpaFacil.Contracts;

public sealed record RegisterRequest(string Name, string Email, string Password,
    CustomerType CustomerType = CustomerType.Individual, string? TaxId = null);
public sealed record LoginRequest(string Email, string Password, string? Device = null);
public sealed record RefreshRequest(string RefreshToken, string? Device = null);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, UserSummary User);
public sealed record UserSummary(Guid Id, string Name, string Email, UserRole Role);

public sealed record CategoryDto(Guid Id, string Name, string? Description, bool IsActive);
public sealed record SaveCategoryRequest(string Name, string? Description, bool IsActive = true);
public sealed record ProductDto(Guid Id, Guid CategoryId, string CategoryName, string Sku, string Name,
    string? Description, string Brand, string Presentation, string? UsageInstructions, decimal Price,
    int StockAvailable, string? ImageUrl, bool IsActive, long Version);
public sealed record SaveProductRequest(Guid CategoryId, string Sku, string Name, string? Description,
    string Brand, string Presentation, string? UsageInstructions, decimal Price, string? ImageUrl,
    bool IsActive = true, long? ExpectedVersion = null);

public sealed record CheckoutItemRequest(Guid ProductId, int Quantity, decimal ConfirmedUnitPrice);
public sealed record CheckoutRequest(string IdempotencyKey, IReadOnlyList<CheckoutItemRequest> Items,
    string? Observation = null);
public sealed record OrderItemDto(Guid ProductId, string Sku, string ProductName, int Quantity,
    decimal UnitPrice, decimal Subtotal);
public sealed record OrderHistoryDto(OrderStatus? PreviousStatus, OrderStatus NewStatus,
    string AuthorName, DateTimeOffset CreatedAt, string? Note);
public sealed record OrderDto(Guid Id, string Number, Guid ClientId, string ClientName, OrderStatus Status,
    decimal Total, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, bool PaymentConfirmed,
    string? Observation, long Version, IReadOnlyList<OrderItemDto> Items, IReadOnlyList<OrderHistoryDto> History);
public sealed record UpdateOrderStatusRequest(OrderStatus NewStatus, long ExpectedVersion, string? Note, bool ConfirmPayment = false);
public sealed record CancelOrderRequest(long ExpectedVersion, string? Note = null);
public sealed record StockAdjustmentRequest(int Quantity, string Reason, long ExpectedProductVersion);
public sealed record CreateEmployeeRequest(string Name, string Email, string Password, UserRole Role);
public sealed record EmployeeDto(Guid Id, string Name, string Email, UserRole Role, bool IsActive);
public sealed record SetUserActiveRequest(bool IsActive);
public sealed record ApiError(string Code, string Message, IReadOnlyDictionary<string, string[]>? Errors = null);
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

using LimpaFacil.Contracts;
using LimpaFacil.Domain;

namespace LimpaFacil.Mobile.Services;

public interface IShopApi
{
    Task<AuthResponse> LoginAsync(string email, string password);
    Task<AuthResponse> RegisterAsync(string name, string email, string password, CustomerType customerType, string taxId);
    Task<List<ProductDto>> GetProductsAsync(string? search = null, Guid? categoryId = null);
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<OrderDto> CheckoutAsync(CheckoutRequest request);
    Task<List<OrderDto>> GetOrdersAsync();
    Task<OrderDto> CancelAsync(Guid id, long version);
    Task<bool> TryRefreshSessionAsync();
}
public interface IUserSession
{
    Task<Guid?> GetUserIdAsync();
    void Clear();
}
public interface IAppNavigation
{
    Task InitializeAsync();
    void ShowLogin();
    void ShowRegistration();
    void ShowShop();
    Task LogoutAsync();
    Task OpenCategoryAsync(CategoryDto category);
    Task OpenProductAsync(ProductDto product);
    Task OpenCartAsync();
}
public interface IUserDialogs
{
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}
public interface INetworkStatus { bool IsOnline { get; } }

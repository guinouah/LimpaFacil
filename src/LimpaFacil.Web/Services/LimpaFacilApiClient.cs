using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LimpaFacil.Contracts;
using LimpaFacil.Domain;

namespace LimpaFacil.Web.Services;

public sealed class LimpaFacilApiClient(HttpClient http)
{
    public UserSummary? User { get; private set; }
    public bool IsLoggedIn => User is not null;

    public async Task LoginAsync(string email, string password)
    {
        var response = await http.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password, "Painel web"));
        await EnsureSuccess(response, "E-mail ou senha inválidos.");
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>() ?? throw new InvalidOperationException("Resposta inválida da API.");
        if (auth.User.Role is not (UserRole.Employee or UserRole.Administrator))
            throw new InvalidOperationException("Este painel é exclusivo para funcionários e administradores. Clientes devem usar o aplicativo.");
        User = auth.User;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
    }

    public void Logout()
    {
        User = null;
        http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<UserSummary> RegisterCustomerAsync(RegisterRequest request)
    {
        using var response = await http.PostAsJsonAsync("api/auth/register", request);
        await EnsureSuccess(response);
        var registered = await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("Resposta inválida da API.");
        // Cadastro público não inicia uma sessão administrativa no painel.
        return registered.User;
    }

    public Task<List<CategoryDto>?> GetCategoriesAsync() => http.GetFromJsonAsync<List<CategoryDto>>("api/admin/categories");
    public Task<List<ProductDto>?> GetProductsAsync() => http.GetFromJsonAsync<List<ProductDto>>("api/admin/products");
    public Task<PagedResponse<OrderDto>?> GetOrdersAsync(OrderStatus? status = null) =>
        http.GetFromJsonAsync<PagedResponse<OrderDto>>(status is null ? "api/admin/orders" : $"api/admin/orders?status={status}");

    public async Task<CategoryDto> SaveCategoryAsync(Guid? id, SaveCategoryRequest model)
    {
        var response = id is null
            ? await http.PostAsJsonAsync("api/admin/categories", model)
            : await http.PutAsJsonAsync($"api/admin/categories/{id}", model);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    public async Task<ProductDto> SaveProductAsync(Guid? id, SaveProductRequest model)
    {
        var response = id is null
            ? await http.PostAsJsonAsync("api/admin/products", model)
            : await http.PutAsJsonAsync($"api/admin/products/{id}", model);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    public async Task<ProductDto> AdjustStockAsync(Guid id, StockAdjustmentRequest request)
    {
        var response = await http.PostAsJsonAsync($"api/admin/products/{id}/stock-adjustments", request);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    public async Task<OrderDto> ChangeOrderStatusAsync(Guid id, UpdateOrderStatusRequest request)
    {
        var response = await http.PutAsJsonAsync($"api/admin/orders/{id}/status", request);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    public async Task<UserSummary> CreateEmployeeAsync(CreateEmployeeRequest request)
    {
        var response = await http.PostAsJsonAsync("api/admin/employees", request);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<UserSummary>())!;
    }

    public Task<List<EmployeeDto>?> GetEmployeesAsync() => http.GetFromJsonAsync<List<EmployeeDto>>("api/admin/employees");
    public async Task SetEmployeeActiveAsync(Guid id, bool active)
    {
        using var response = await http.PutAsJsonAsync($"api/admin/employees/{id}/active", new SetUserActiveRequest(active));
        await EnsureSuccess(response);
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, string? unauthorizedMessage = null)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new InvalidOperationException(unauthorizedMessage ?? "Sua sessão expirou. Entre novamente no painel.");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new InvalidOperationException("Seu perfil não tem permissão para esta operação.");
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            foreach (var key in new[] { "message", "detail", "title" })
                if (json.RootElement.TryGetProperty(key, out var value) && !string.IsNullOrWhiteSpace(value.GetString()))
                    throw new InvalidOperationException(value.GetString());
        }
        catch (JsonException) { }
        throw new InvalidOperationException($"A API respondeu {(int)response.StatusCode}.");
    }
}

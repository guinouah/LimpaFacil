using System.Net.Http.Headers;
using System.Net.Http.Json;
using LimpaFacil.Contracts;
using LimpaFacil.Domain;

namespace LimpaFacil.Mobile.Services;

public sealed class ApiClient(HttpClient http, TokenStore tokens) : IShopApi
{
    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest(email, password, DeviceDescription()))
        };
        using var response = await SendAsync(request);
        var auth = await Read<AuthResponse>(response); await tokens.SaveAsync(auth); return auth;
    }
    public async Task<AuthResponse> RegisterAsync(string name, string email, string password, CustomerType customerType, string taxId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/register")
        {
            Content = JsonContent.Create(new RegisterRequest(name, email, password, customerType, taxId))
        };
        using var response = await SendAsync(request);
        var auth = await Read<AuthResponse>(response); await tokens.SaveAsync(auth); return auth;
    }
    public async Task<List<ProductDto>> GetProductsAsync(string? search = null, Guid? categoryId = null)
    {
        var url = $"api/catalog/products?pageSize=100&search={Uri.EscapeDataString(search ?? "")}" + (categoryId is null ? "" : $"&categoryId={categoryId}");
        var products = new List<ProductDto>();
        for (var page = 1; ; page++)
        {
            using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url + $"&page={page}"));
            var result = await Read<PagedResponse<ProductDto>>(response);
            products.AddRange(result.Items);
            if (result.Items.Count == 0 || products.Count >= result.Total) return products;
        }
    }
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, "api/catalog/categories"));
        return await Read<List<CategoryDto>>(response);
    }
    public async Task<OrderDto> CheckoutAsync(CheckoutRequest request) => await Authorized<OrderDto>(HttpMethod.Post, "api/orders", request);
    public async Task<List<OrderDto>> GetOrdersAsync() => await Authorized<List<OrderDto>>(HttpMethod.Get, "api/orders");
    public async Task<OrderDto> CancelAsync(Guid id, long version) => await Authorized<OrderDto>(HttpMethod.Post, $"api/orders/{id}/cancel", new CancelOrderRequest(version));
    public async Task<bool> TryRefreshSessionAsync()
    {
        var refreshToken = await tokens.GetRefreshAsync();
        if (string.IsNullOrWhiteSpace(refreshToken)) return false;
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh")
        {
            Content = JsonContent.Create(new RefreshRequest(refreshToken, DeviceDescription()))
        };
        using var response = await SendAsync(request);
        if (!response.IsSuccessStatusCode) return false;
        await tokens.SaveAsync(await Read<AuthResponse>(response));
        return true;
    }
    private async Task<T> Authorized<T>(HttpMethod method, string url, object? body = null)
    {
        using var response = await SendAuthorizedAsync(method, url, body);
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) return await Read<T>(response);
        if (!await TryRefreshSessionAsync()) throw new InvalidOperationException("Sua sessão expirou. Saia e entre novamente com seu e-mail e senha.");
        using var retry = await SendAuthorizedAsync(method, url, body);
        return await Read<T>(retry);
    }
    private async Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string url, object? body)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetAccessAsync());
        if (body is not null) request.Content = JsonContent.Create(body);
        return await SendAsync(request);
    }
    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        try
        {
            return await http.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            throw new InvalidOperationException("Não foi possível conectar à loja. Verifique sua conexão e tente novamente.");
        }
        catch (TaskCanceledException)
        {
            throw new InvalidOperationException("A API demorou demais para responder. Tente novamente.");
        }
    }
    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return (await response.Content.ReadFromJsonAsync<T>())!;
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            throw new InvalidOperationException(error?.Message ?? "Falha na comunicação com a loja.");
        }
        catch (System.Text.Json.JsonException)
        {
            throw new InvalidOperationException("Falha na comunicação com a loja.");
        }
    }
    private static string DeviceDescription() => $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.Model}";
}

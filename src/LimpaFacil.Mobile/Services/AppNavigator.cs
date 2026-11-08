using LimpaFacil.Contracts;
using LimpaFacil.Mobile.Pages;
using LimpaFacil.Mobile.ViewModels;

namespace LimpaFacil.Mobile.Services;

/// <summary>Único ponto responsável por criar telas e navegar na interface MAUI.</summary>
public sealed class AppNavigator(IServiceProvider services) : IAppNavigation
{
    private Page Root { get => Application.Current!.Windows[0].Page!; set => Application.Current!.Windows[0].Page = value; }
    public Page CreateInitialPage() => services.GetRequiredService<StartupPage>();
    public async Task InitializeAsync()
    {
        var session = services.GetRequiredService<IUserSession>();
        if (await session.GetUserIdAsync() is not null)
        {
            try
            {
                if (await services.GetRequiredService<IShopApi>().TryRefreshSessionAsync()) { ShowShop(); return; }
            }
            catch (InvalidOperationException)
            {
                // Falha de conexão não apaga a identidade local: permite consultar o cache.
                // Escritas continuam dependendo da autenticação e das regras da API.
                ShowShop();
                return;
            }
            session.Clear();
        }
        ShowLogin();
    }
    public void ShowShop() => Root = services.GetRequiredService<ShopTabs>();
    public void ShowLogin() => Root = new NavigationPage(services.GetRequiredService<LoginPage>());
    public void ShowRegistration() => Root = new NavigationPage(services.GetRequiredService<RegistrationPage>());
    public async Task LogoutAsync()
    {
        var session = services.GetRequiredService<IUserSession>();
        var id = await session.GetUserIdAsync();
        if (id.HasValue) await services.GetRequiredService<LocalStore>().ClearPrivateAsync(id.Value);
        session.Clear();
        ShowLogin();
    }
    private INavigation CurrentNavigation => (Root is TabbedPage tabs ? tabs.CurrentPage! : Root).Navigation;
    public Task OpenCategoryAsync(CategoryDto category)
    {
        var viewModel = services.GetRequiredService<CatalogViewModel>();
        viewModel.SetCategory(category);
        return CurrentNavigation.PushAsync(new CatalogPage(viewModel));
    }
    public Task OpenProductAsync(ProductDto product)
    {
        var viewModel = services.GetRequiredService<ProductViewModel>();
        viewModel.SetProduct(product);
        return CurrentNavigation.PushAsync(new ProductPage(viewModel));
    }
    public Task OpenCartAsync() => CurrentNavigation.PushAsync(services.GetRequiredService<CartPage>());
}

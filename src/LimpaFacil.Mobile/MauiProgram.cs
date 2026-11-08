using LimpaFacil.Mobile.Services;
using LimpaFacil.Mobile.Pages;
using LimpaFacil.Mobile.ViewModels;

namespace LimpaFacil.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
        var builder = MauiApp.CreateBuilder().UseMauiApp<App>();
        builder.Services.AddSingleton<LocalStore>();
        builder.Services.AddSingleton<TokenStore>();
        var apiBaseAddress =
#if ANDROID
            "http://10.0.2.2:5100/";
#else
            "http://localhost:5100/";
#endif
        builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(apiBaseAddress), Timeout = TimeSpan.FromSeconds(15) });
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<IShopApi>(sp => sp.GetRequiredService<ApiClient>());
        builder.Services.AddSingleton<IUserSession>(sp => sp.GetRequiredService<TokenStore>());
        builder.Services.AddSingleton<AppNavigator>();
        builder.Services.AddSingleton<IAppNavigation>(sp => sp.GetRequiredService<AppNavigator>());
        builder.Services.AddSingleton<IUserDialogs, UserDialogs>();
        builder.Services.AddSingleton<INetworkStatus, NetworkStatus>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegistrationViewModel>();
        builder.Services.AddTransient<StartupViewModel>();
        builder.Services.AddTransient<CategoriesViewModel>();
        builder.Services.AddTransient<CatalogViewModel>();
        builder.Services.AddTransient<ProductViewModel>();
        builder.Services.AddTransient<CartViewModel>();
        builder.Services.AddTransient<MyOrdersViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegistrationPage>();
        builder.Services.AddTransient<StartupPage>();
        builder.Services.AddTransient<CategoriesPage>();
        builder.Services.AddTransient<CatalogPage>();
        builder.Services.AddTransient<ProductPage>();
        builder.Services.AddTransient<CartPage>();
        builder.Services.AddTransient<MyOrdersPage>();
        builder.Services.AddTransient<ShopTabs>();
        return builder.Build();
    }
}

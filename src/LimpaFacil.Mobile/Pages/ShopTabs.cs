using LimpaFacil.Mobile.Ui;

namespace LimpaFacil.Mobile.Pages;

public sealed class ShopTabs : TabbedPage
{
    public ShopTabs(HomePage home, CategoriesPage categories, CatalogPage products, MyOrdersPage orders)
    {
        Title = "LimpaFácil";
        BarBackgroundColor = Colors.White;
        BarTextColor = SelectedTabColor = Theme.Green;
        UnselectedTabColor = Theme.Muted;
        Children.Add(new NavigationPage(home) { Title = "Início" });
        Children.Add(new NavigationPage(categories) { Title = "Categorias" });
        Children.Add(new NavigationPage(products) { Title = "Produtos" });
        Children.Add(new NavigationPage(orders) { Title = "Meus pedidos" });
    }
}

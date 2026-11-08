using LimpaFacil.Mobile.Ui;
using LimpaFacil.Mobile.ViewModels;

namespace LimpaFacil.Mobile.Pages;

public sealed class CatalogPage : ContentPage
{
    private readonly CatalogViewModel viewModel;
    public CatalogPage(CatalogViewModel viewModel)
    {
        this.viewModel = viewModel;
        BindingContext = viewModel;
        this.SetBinding(TitleProperty, nameof(CatalogViewModel.Title));
        BackgroundColor = Theme.Background;
        ToolbarItems.Add(BoundUi.Toolbar("Carrinho", nameof(CatalogViewModel.OpenCartCommand)));
        ToolbarItems.Add(BoundUi.Toolbar("Atualizar", nameof(CatalogViewModel.LoadCommand)));
        var search = new SearchBar { Placeholder = "Buscar produto, marca ou código", BackgroundColor = Colors.White, TextColor = Theme.Text };
        search.SetBinding(SearchBar.TextProperty, nameof(CatalogViewModel.Search), BindingMode.TwoWay);
        var categories = new Picker { Title = "Todas as categorias", TextColor = Theme.Text, BackgroundColor = Colors.White };
        categories.SetBinding(Picker.ItemsSourceProperty, nameof(CatalogViewModel.CategoryNames));
        categories.SetBinding(Picker.SelectedIndexProperty, nameof(CatalogViewModel.SelectedCategoryIndex), BindingMode.TwoWay);
        categories.SetBinding(IsVisibleProperty, nameof(CatalogViewModel.ShowCategoryFilter));
        var heading = new VerticalStackLayout
        {
            Spacing = 8,
            Children = { BoundUi.Text(nameof(CatalogViewModel.Title), 24, Theme.Text, bold: true), search, categories,
                BoundUi.Text(nameof(CatalogViewModel.Message), 12, Theme.Muted) }
        };
        Content = BoundUi.ListPage(heading, BoundUi.Collection(nameof(CatalogViewModel.Products), () =>
        {
            var button = BoundUi.Button("Ver e adicionar", nameof(ProductItemViewModel.OpenCommand));
            button.SetBinding(Button.TextProperty, nameof(ProductItemViewModel.ButtonText));
            return Theme.Card(new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    BoundUi.Text("Product.CategoryName", 11, Theme.Cyan),
                    BoundUi.Text("Product.Name", 19, Theme.Text, bold: true),
                    BoundUi.Text("Product.Brand", 12, Theme.Muted),
                    BoundUi.Text("Product.Presentation", 12, Theme.Muted),
                    BoundUi.Text("Product.Price", 21, Theme.Green, "{0:C}", true),
                    BoundUi.Text("Product.StockAvailable", 11, Theme.Muted, "{0} em estoque"),
                    button, BoundUi.Text(nameof(ProductItemViewModel.Message), 12, Colors.Firebrick)
                }
            });
        }, "Nenhum produto encontrado."));
    }
    protected override async void OnAppearing() { base.OnAppearing(); await viewModel.LoadCommand.ExecuteAsync(); }
}

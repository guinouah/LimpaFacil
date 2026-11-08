using LimpaFacil.Mobile.Ui;
using LimpaFacil.Mobile.ViewModels;

namespace LimpaFacil.Mobile.Pages;

public sealed class CategoriesPage : ContentPage
{
    private readonly CategoriesViewModel viewModel;
    public CategoriesPage(CategoriesViewModel viewModel)
    {
        this.viewModel = viewModel;
        BindingContext = viewModel;
        Title = "Categorias";
        BackgroundColor = Theme.Background;
        ToolbarItems.Add(BoundUi.Toolbar("Sair", nameof(CategoriesViewModel.LogoutCommand)));
        var heading = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "O que você precisa limpar hoje?", FontSize = 25, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text },
                BoundUi.Text(nameof(CategoriesViewModel.Message), 12, Theme.Muted)
            }
        };
        Content = BoundUi.ListPage(heading, BoundUi.Collection(nameof(CategoriesViewModel.Categories), () =>
            Theme.Card(new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    BoundUi.Text("Category.Name", 19, Theme.Blue, bold: true),
                    BoundUi.Text("Category.Description", 12, Theme.Muted),
                    BoundUi.Text(nameof(CategoryItemViewModel.ProductCount), 12, Theme.Green, "{0} produto(s) disponível(is)"),
                    BoundUi.Button("Ver produtos", nameof(CategoryItemViewModel.OpenCommand), true),
                    BoundUi.Text(nameof(CategoryItemViewModel.Message), 12, Colors.Firebrick)
                }
            }), "Nenhuma categoria disponível."));
    }
    protected override async void OnAppearing() { base.OnAppearing(); await viewModel.LoadCommand.ExecuteAsync(); }
}

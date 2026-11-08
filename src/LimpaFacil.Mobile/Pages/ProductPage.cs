using LimpaFacil.Mobile.Ui;
using LimpaFacil.Mobile.ViewModels;

namespace LimpaFacil.Mobile.Pages;

public sealed class ProductPage : ContentPage
{
    public ProductPage(ProductViewModel viewModel)
    {
        BindingContext = viewModel;
        this.SetBinding(TitleProperty, "Product.Name");
        BackgroundColor = Theme.Background;
        var less = BoundUi.Button("−", nameof(ProductViewModel.DecreaseCommand), secondary: true);
        var more = BoundUi.Button("+", nameof(ProductViewModel.IncreaseCommand), secondary: true);
        less.WidthRequest = more.WidthRequest = 54;
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 18, Spacing = 14,
                Children =
                {
                    Theme.Card(new VerticalStackLayout
                    {
                        Spacing = 8,
                        Children =
                        {
                            BoundUi.Text("Product.CategoryName", 12, Theme.Cyan),
                            BoundUi.Text("Product.Name", 28, Theme.Text, bold: true),
                            BoundUi.Text("Product.Brand", 14, Theme.Muted),
                            BoundUi.Text("Product.Presentation", 14, Theme.Muted),
                            BoundUi.Text("Product.Price", 25, Theme.Green, "{0:C}", true),
                            BoundUi.Text("Product.Description", 14, Theme.Text),
                            new Label { Text = "Orientações de uso", FontAttributes = FontAttributes.Bold, TextColor = Theme.Text },
                            BoundUi.Text("Product.UsageInstructions", 12, Theme.Muted)
                        }
                    }),
                    new Label { Text = "Quantidade", FontAttributes = FontAttributes.Bold, TextColor = Theme.Text },
                    new HorizontalStackLayout { Spacing = 18, HorizontalOptions = LayoutOptions.Center,
                        Children = { less, BoundUi.Text(nameof(ProductViewModel.Quantity), 20, Theme.Text, bold: true), more } },
                    BoundUi.Button("Adicionar ao carrinho", nameof(ProductViewModel.AddCommand)),
                    BoundUi.Text(nameof(ProductViewModel.Message), 12, Theme.Green)
                }
            }
        };
    }
}

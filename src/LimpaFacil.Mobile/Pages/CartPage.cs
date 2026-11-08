using LimpaFacil.Mobile.Ui;
using LimpaFacil.Mobile.ViewModels;

namespace LimpaFacil.Mobile.Pages;

public sealed class CartPage : ContentPage
{
    private readonly CartViewModel viewModel;
    public CartPage(CartViewModel viewModel)
    {
        this.viewModel = viewModel;
        BindingContext = viewModel;
        Title = "Meu carrinho";
        BackgroundColor = Theme.Background;
        var observation = new Editor { Placeholder = "Observações para o pedido (opcional)", HeightRequest = 76,
            MaxLength = 500, BackgroundColor = Colors.White, TextColor = Theme.Text };
        observation.SetBinding(Editor.TextProperty, nameof(CartViewModel.Observation), BindingMode.TwoWay);
        var list = BoundUi.Collection(nameof(CartViewModel.Items), () =>
        {
            var less = BoundUi.Button("−", nameof(CartItemViewModel.DecreaseCommand), true);
            var more = BoundUi.Button("+", nameof(CartItemViewModel.IncreaseCommand), true);
            less.WidthRequest = more.WidthRequest = 48;
            return Theme.Card(new VerticalStackLayout
            {
                Spacing = 7,
                Children =
                {
                    BoundUi.Text("Row.Name", 17, Theme.Text, bold: true),
                    BoundUi.Text("Row.UnitPrice", 12, Theme.Muted, "{0:C} cada"),
                    new HorizontalStackLayout { Spacing = 12, Children = { less, BoundUi.Text("Row.Quantity", 16), more } },
                    BoundUi.Text(nameof(CartItemViewModel.Subtotal), 14, Theme.Blue, "Subtotal: {0:C}", true),
                    BoundUi.Text(nameof(CartItemViewModel.Message), 12, Colors.Firebrick)
                }
            }, 13);
        }, "Seu carrinho está vazio.");
        var actions = Theme.Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                BoundUi.Text(nameof(CartViewModel.Total), 22, Theme.Green, "Total: {0:C}", true),
                new Label { Text = "Pagamento no momento da retirada.", FontSize = 11, TextColor = Theme.Muted },
                observation, BoundUi.Button("Confirmar pedido para retirada", nameof(CartViewModel.CheckoutCommand)),
                BoundUi.Text(nameof(CartViewModel.Message), 12, Theme.Muted)
            }
        }, 14);
        var grid = new Grid { Padding = 14, RowSpacing = 10, RowDefinitions = [new(GridLength.Star), new(GridLength.Auto)] };
        grid.Add(list, 0, 0);
        grid.Add(actions, 0, 1);
        Content = grid;
    }
    protected override async void OnAppearing() { base.OnAppearing(); await viewModel.LoadCommand.ExecuteAsync(); }
}

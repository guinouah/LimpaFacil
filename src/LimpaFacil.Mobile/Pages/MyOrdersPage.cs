using LimpaFacil.Mobile.Ui;
using LimpaFacil.Mobile.ViewModels;

namespace LimpaFacil.Mobile.Pages;

public sealed class MyOrdersPage : ContentPage
{
    private readonly MyOrdersViewModel viewModel;
    public MyOrdersPage(MyOrdersViewModel viewModel)
    {
        this.viewModel = viewModel;
        BindingContext = viewModel;
        Title = "Meus pedidos";
        BackgroundColor = Theme.Background;
        ToolbarItems.Add(BoundUi.Toolbar("Atualizar", nameof(MyOrdersViewModel.LoadCommand)));
        ToolbarItems.Add(BoundUi.Toolbar("Sair", nameof(MyOrdersViewModel.LogoutCommand)));
        var heading = new VerticalStackLayout { Spacing = 10, Children =
        {
            new Label { Text = "Acompanhe seus pedidos", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text },
            BoundUi.Text(nameof(MyOrdersViewModel.Message), 12, Theme.Muted)
        }};
        Content = BoundUi.ListPage(heading, BoundUi.Collection(nameof(MyOrdersViewModel.Orders), () =>
        {
            var status = BoundUi.Text(nameof(OrderItemViewModel.StatusText), 13, bold: true);
            status.SetBinding(Label.TextColorProperty, nameof(OrderItemViewModel.StatusColor));
            var cancel = BoundUi.Button("Cancelar pedido", nameof(OrderItemViewModel.CancelCommand), true);
            cancel.TextColor = Colors.Firebrick;
            cancel.SetBinding(IsVisibleProperty, nameof(OrderItemViewModel.CanCancel));
            return Theme.Card(new VerticalStackLayout
            {
                Spacing = 7,
                Children =
                {
                    BoundUi.Text("Order.Number", 17, Theme.Text, bold: true), status,
                    BoundUi.Text("Order.CreatedAt", 11, Theme.Muted, "{0:dd/MM/yyyy HH:mm}"),
                    BoundUi.Text("Order.Total", 18, Theme.Green, "{0:C}", true),
                    BoundUi.Text(nameof(OrderItemViewModel.ItemsText), 12, Theme.Muted),
                    BoundUi.Text("Order.Observation", 12, Theme.Muted, "Observação: {0}"),
                    BoundUi.Text(nameof(OrderItemViewModel.HistoryText), 11, Theme.Muted),
                    BoundUi.Button("Repetir este pedido", nameof(OrderItemViewModel.RepeatCommand), true), cancel,
                    BoundUi.Text(nameof(OrderItemViewModel.Message), 12, Colors.Firebrick)
                }
            }, 14);
        }, "Você ainda não tem pedidos."));
    }
    protected override async void OnAppearing() { base.OnAppearing(); await viewModel.LoadCommand.ExecuteAsync(); }
}

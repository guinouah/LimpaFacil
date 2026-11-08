using System.Collections.ObjectModel;
using LimpaFacil.Contracts;
using LimpaFacil.Domain;
using LimpaFacil.Mobile.Mvvm;
using LimpaFacil.Mobile.Services;

namespace LimpaFacil.Mobile.ViewModels;

public sealed class OrderItemViewModel : BaseViewModel
{
    public OrderDto Order { get; }
    public string StatusText => Order.Status.ToPtBr();
    public string StatusColor => Order.Status switch
    {
        OrderStatus.Received => "#B45309", OrderStatus.InSeparation => "#0284C7",
        OrderStatus.ReadyForPickup => "#1E40AF", OrderStatus.Completed => "#16A34A", _ => "#DC2626"
    };
    public bool CanCancel => Order.Status == OrderStatus.Received;
    public string ItemsText => string.Join(Environment.NewLine, Order.Items.Select(x => $"• {x.Quantity} × {x.ProductName}"));
    public string HistoryText => string.Join(Environment.NewLine, Order.History.Select(x =>
        $"{x.CreatedAt.LocalDateTime:dd/MM HH:mm} · {x.NewStatus.ToPtBr()} · {x.AuthorName}"));
    public AsyncCommand CancelCommand { get; }
    public AsyncCommand RepeatCommand { get; }
    public OrderItemViewModel(OrderDto order, Func<OrderDto, Task> cancel, Func<OrderDto, Task> repeat)
    {
        Order = order;
        CancelCommand = Command(() => cancel(order), () => CanCancel);
        RepeatCommand = Command(() => repeat(order));
    }
}

public sealed class MyOrdersViewModel : BaseViewModel
{
    private readonly IShopApi api;
    private readonly LocalStore store;
    private readonly IUserSession session;
    private readonly IAppNavigation navigation;
    private readonly IUserDialogs dialogs;
    public ObservableCollection<OrderItemViewModel> Orders { get; } = [];
    public AsyncCommand LoadCommand { get; }
    public AsyncCommand LogoutCommand { get; }
    public MyOrdersViewModel(IShopApi api, LocalStore store, IUserSession session, IAppNavigation navigation, IUserDialogs dialogs)
    {
        this.api = api; this.store = store; this.session = session; this.navigation = navigation; this.dialogs = dialogs;
        LoadCommand = Command(LoadAsync);
        LogoutCommand = Command(navigation.LogoutAsync);
    }
    private async Task LoadAsync()
    {
        Message = "Atualizando...";
        var orders = await api.GetOrdersAsync();
        Orders.Clear();
        foreach (var order in orders) Orders.Add(new(order, CancelAsync, RepeatAsync));
        Message = Orders.Count == 0 ? "Você ainda não tem pedidos. Abra Produtos para começar." : "";
    }
    private Task CancelAsync(OrderDto order) => RunAsync(async () =>
    {
        if (!await dialogs.ConfirmAsync("Cancelar pedido", "Deseja realmente cancelar este pedido?", "Sim, cancelar", "Não")) return;
        await api.CancelAsync(order.Id, order.Version);
        await LoadAsync();
    });
    private Task RepeatAsync(OrderDto order) => RunAsync(async () =>
    {
        if (!await dialogs.ConfirmAsync("Repetir pedido", "O carrinho atual será substituído. Continuar?", "Repetir", "Cancelar")) return;
        var id = await session.GetUserIdAsync() ?? throw new InvalidOperationException("Faça login novamente.");
        List<ProductDto> products;
        try { products = await api.GetProductsAsync(); await store.SaveCatalogAsync(products); }
        catch { products = (await store.GetCatalogAsync()).Products; }
        var result = await store.ReplaceCartFromOrderAsync(id, order.Items, products);
        if (result.Added == 0) throw new InvalidOperationException("Nenhum produto deste pedido continua disponível.");
        Message = result.Skipped == 0 ? "Pedido copiado. Revise o carrinho." : $"Pedido copiado; {result.Skipped} item(ns) indisponível(is) foram ignorados.";
        await navigation.OpenCartAsync();
    });
}

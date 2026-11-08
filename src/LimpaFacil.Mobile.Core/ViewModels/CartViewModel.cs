using System.Collections.ObjectModel;
using LimpaFacil.Contracts;
using LimpaFacil.Mobile.Mvvm;
using LimpaFacil.Mobile.Services;

namespace LimpaFacil.Mobile.ViewModels;

public sealed class CartItemViewModel : BaseViewModel
{
    public CartRow Row { get; }
    public decimal Subtotal => Row.UnitPrice * Row.Quantity;
    public AsyncCommand IncreaseCommand { get; }
    public AsyncCommand DecreaseCommand { get; }
    public CartItemViewModel(CartRow row, Func<CartRow, int, Task> update)
    {
        Row = row;
        IncreaseCommand = Command(() => update(Row, Row.Quantity + 1));
        DecreaseCommand = Command(() => update(Row, Row.Quantity - 1));
    }
}

public sealed class CartViewModel : BaseViewModel
{
    private readonly LocalStore store;
    private readonly IUserSession session;
    private string observation = "";
    private decimal total;
    public ObservableCollection<CartItemViewModel> Items { get; } = [];
    public decimal Total { get => total; private set => SetProperty(ref total, value); }
    public string Observation { get => observation; set => SetProperty(ref observation, value); }
    public AsyncCommand LoadCommand { get; }
    public AsyncCommand CheckoutCommand { get; }

    public CartViewModel(IShopApi api, LocalStore store, IUserSession session, INetworkStatus network)
    {
        this.store = store;
        this.session = session;
        LoadCommand = Command(LoadAsync);
        CheckoutCommand = Command(async () =>
        {
            if (!network.IsOnline) throw new InvalidOperationException("É necessário estar conectado para confirmar o pedido.");
            if (Observation.Length > 500) throw new InvalidOperationException("A observação deve ter até 500 caracteres.");
            var id = await session.GetUserIdAsync() ?? throw new InvalidOperationException("Sessão inválida.");
            var cart = await store.GetCartAsync(id);
            if (cart.Count == 0) throw new InvalidOperationException("O carrinho está vazio.");
            Message = "Enviando pedido...";
            var key = await store.GetCheckoutKeyAsync(id, cart);
            var order = await api.CheckoutAsync(new CheckoutRequest(key,
                cart.Select(x => new CheckoutItemRequest(Guid.Parse(x.ProductId), x.Quantity, x.UnitPrice)).ToArray(), Observation.Trim()));
            await store.ClearCartAsync(id);
            Observation = "";
            await LoadAsync();
            Message = $"✓ Pedido {order.Number} registrado com sucesso.";
        }, () => Items.Count > 0);
    }
    private async Task LoadAsync()
    {
        var id = await session.GetUserIdAsync() ?? throw new InvalidOperationException("Faça login novamente.");
        var cart = await store.GetCartAsync(id);
        Items.Clear();
        foreach (var row in cart) Items.Add(new(row, UpdateAsync));
        Total = cart.Sum(x => x.Quantity * x.UnitPrice);
        CheckoutCommand.NotifyCanExecuteChanged();
    }
    private Task UpdateAsync(CartRow row, int quantity) => RunAsync(async () =>
    {
        await store.UpdateQuantityAsync(row, quantity);
        await LoadAsync();
    });
}

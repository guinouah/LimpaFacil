using LimpaFacil.Contracts;
using LimpaFacil.Domain;
using LimpaFacil.Mobile.Mvvm;
using LimpaFacil.Mobile.Services;
using LimpaFacil.Mobile.ViewModels;

namespace LimpaFacil.UnitTests;

public sealed class ViewModelTests
{
    [Fact]
    public async Task Login_validates_input_without_contacting_api()
    {
        var api = new FakeApi(); var navigation = new FakeNavigation();
        var vm = new LoginViewModel(api, navigation);
        await vm.LoginCommand.ExecuteAsync();
        Assert.Equal(0, api.LoginCalls);
        Assert.Contains("Informe", vm.Message);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Login_notifies_bindings_navigates_and_clears_password()
    {
        var api = new FakeApi(); var navigation = new FakeNavigation();
        var vm = new LoginViewModel(api, navigation);
        var changed = new List<string?>(); vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        vm.Email = "  client@test.local  "; vm.Password = "password-123";
        await vm.LoginCommand.ExecuteAsync();
        Assert.Equal("client@test.local", api.LastEmail);
        Assert.True(navigation.ShopOpened);
        Assert.Empty(vm.Password);
        Assert.Contains(nameof(vm.IsBusy), changed);
        Assert.Contains(nameof(vm.Email), changed);
    }

    [Fact]
    public async Task Failed_login_reports_message_without_navigation()
    {
        var navigation = new FakeNavigation();
        var vm = new LoginViewModel(new FakeApi { FailLogin = true }, navigation) { Email = "client@test.local", Password = "password-123" };
        await vm.LoginCommand.ExecuteAsync();
        Assert.Equal("Credenciais inválidas", vm.Message);
        Assert.False(navigation.ShopOpened);
        Assert.True(vm.LoginCommand.CanExecute(null));
    }

    [Fact]
    public async Task Login_register_command_opens_registration()
    {
        var navigation = new FakeNavigation();
        var vm = new LoginViewModel(new FakeApi(), navigation);
        await vm.RegisterCommand.ExecuteAsync();
        Assert.True(navigation.RegistrationOpened);
    }

    [Fact]
    public async Task Registration_sends_customer_data_and_opens_shop()
    {
        var api = new FakeApi(); var navigation = new FakeNavigation();
        var vm = new RegistrationViewModel(api, navigation)
        {
            Name = "  Loja Teste  ", Email = "  loja@test.local  ", Password = "password-123",
            TaxId = "12345678000195", CustomerTypeIndex = 1
        };
        await vm.RegisterCommand.ExecuteAsync();
        Assert.Equal("Loja Teste", api.RegisteredName);
        Assert.Equal(CustomerType.Company, api.RegisteredCustomerType);
        Assert.Equal("loja@test.local", api.LastEmail);
        Assert.True(navigation.ShopOpened);
        Assert.Empty(vm.Password);
    }

    [Fact]
    public void Company_selection_updates_document_and_name_bindings()
    {
        var vm = new RegistrationViewModel(new FakeApi(), new FakeNavigation());
        var changed = new List<string?>(); vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        vm.CustomerTypeIndex = 1;
        Assert.Equal("CNPJ", vm.TaxIdPlaceholder);
        Assert.Equal("Razão social", vm.NamePlaceholder);
        Assert.Contains(nameof(vm.TaxIdPlaceholder), changed);
    }

    [Fact]
    public async Task Async_command_blocks_duplicate_execution_and_restores_state()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var command = new AsyncCommand(async () => { calls++; await gate.Task; });
        var first = command.ExecuteAsync();
        Assert.False(command.CanExecute(null));
        await command.ExecuteAsync();
        Assert.Equal(1, calls);
        gate.SetResult(); await first;
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task Offline_checkout_keeps_cart_and_never_calls_api()
    {
        await using var store = Store(); var api = new FakeApi(); var session = new FakeSession();
        await store.AddCartAsync(session.Id, Product(), 2);
        var vm = new CartViewModel(api, store, session, new FakeNetwork(false));
        await vm.LoadCommand.ExecuteAsync();
        await vm.CheckoutCommand.ExecuteAsync();
        Assert.Equal(0, api.CheckoutCalls);
        Assert.Single(await store.GetCartAsync(session.Id));
        Assert.Contains("conectado", vm.Message);
    }

    [Fact]
    public async Task Successful_checkout_sends_observation_and_clears_only_own_cart()
    {
        await using var store = Store(); var api = new FakeApi(); var session = new FakeSession();
        var other = Guid.NewGuid(); var product = Product();
        await store.AddCartAsync(session.Id, product, 2); await store.AddCartAsync(other, product);
        var vm = new CartViewModel(api, store, session, new FakeNetwork(true)) { Observation = "  Separar em sacola  " };
        await vm.LoadCommand.ExecuteAsync();
        Assert.Equal(20m, vm.Total);
        await vm.CheckoutCommand.ExecuteAsync();
        Assert.Equal("Separar em sacola", api.Checkout!.Observation);
        Assert.Empty(await store.GetCartAsync(session.Id));
        Assert.Single(await store.GetCartAsync(other));
        Assert.False(vm.CheckoutCommand.CanExecute(null));
        Assert.Equal(0m, vm.Total);
    }

    [Fact]
    public async Task Catalog_filters_cached_products_when_offline()
    {
        await using var store = Store(); var product = Product();
        await store.SaveCatalogAsync([product, product with { Id = Guid.NewGuid(), Name = "Sabão", Sku = "SAB" }]);
        var vm = new CatalogViewModel(new FakeApi { FailCatalog = true }, store, new FakeNavigation());
        await vm.LoadCommand.ExecuteAsync();
        Assert.Equal(2, vm.Products.Count);
        vm.Search = "desinf";
        Assert.Single(vm.Products);
        Assert.Contains("offline", vm.Message);
    }

    private static LocalStore Store() => new(Path.Combine(Path.GetTempPath(), $"limpafacil_mvvm_{Guid.NewGuid():N}.db"));
    private static ProductDto Product() => new(Guid.NewGuid(), Guid.NewGuid(), "Limpeza", "DES", "Desinfetante", "Descrição", "Marca", "1L", "Diluir", 10m, 10, null, true, 1);
    private sealed class FakeSession : IUserSession
    {
        public Guid Id { get; } = Guid.NewGuid();
        public Task<Guid?> GetUserIdAsync() => Task.FromResult<Guid?>(Id);
        public void Clear() { }
    }
    private sealed record FakeNetwork(bool IsOnline) : INetworkStatus;
    private sealed class FakeNavigation : IAppNavigation
    {
        public bool ShopOpened { get; private set; }
        public bool RegistrationOpened { get; private set; }
        public void ShowShop() => ShopOpened = true;
        public void ShowLogin() { }
        public void ShowRegistration() => RegistrationOpened = true;
        public Task InitializeAsync() => Task.CompletedTask;
        public Task LogoutAsync() => Task.CompletedTask;
        public Task OpenCategoryAsync(CategoryDto category) => Task.CompletedTask;
        public Task OpenProductAsync(ProductDto product) => Task.CompletedTask;
        public Task OpenCartAsync() => Task.CompletedTask;
    }
    private sealed class FakeApi : IShopApi
    {
        public int LoginCalls { get; private set; }
        public int CheckoutCalls { get; private set; }
        public string? LastEmail { get; private set; }
        public bool FailLogin { get; init; }
        public bool FailCatalog { get; init; }
        public CheckoutRequest? Checkout { get; private set; }
        public string? RegisteredName { get; private set; }
        public CustomerType? RegisteredCustomerType { get; private set; }
        public Task<AuthResponse> LoginAsync(string email, string password)
        {
            LoginCalls++; LastEmail = email;
            if (FailLogin) throw new InvalidOperationException("Credenciais inválidas");
            return Task.FromResult(new AuthResponse("token", "refresh", DateTimeOffset.UtcNow.AddMinutes(10), new(Guid.NewGuid(), "Cliente", email, UserRole.Client)));
        }
        public Task<AuthResponse> RegisterAsync(string name, string email, string password, CustomerType customerType, string taxId)
        {
            RegisteredName = name; RegisteredCustomerType = customerType;
            return LoginAsync(email, password);
        }
        public Task<List<ProductDto>> GetProductsAsync(string? search = null, Guid? categoryId = null) => FailCatalog ? throw new InvalidOperationException("Offline") : Task.FromResult(new List<ProductDto>());
        public Task<List<CategoryDto>> GetCategoriesAsync() => Task.FromResult(new List<CategoryDto>());
        public Task<OrderDto> CheckoutAsync(CheckoutRequest request)
        {
            CheckoutCalls++; Checkout = request;
            return Task.FromResult(new OrderDto(Guid.NewGuid(), "LF-TEST", Guid.NewGuid(), "Cliente", OrderStatus.Received,
                20m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, false, request.Observation, 1, [], []));
        }
        public Task<List<OrderDto>> GetOrdersAsync() => Task.FromResult(new List<OrderDto>());
        public Task<OrderDto> CancelAsync(Guid id, long version) => throw new NotSupportedException();
        public Task<bool> TryRefreshSessionAsync() => Task.FromResult(false);
    }
}

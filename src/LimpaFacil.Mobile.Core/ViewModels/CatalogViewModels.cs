using System.Collections.ObjectModel;
using LimpaFacil.Contracts;
using LimpaFacil.Mobile.Mvvm;
using LimpaFacil.Mobile.Services;

namespace LimpaFacil.Mobile.ViewModels;

public sealed class CategoryItemViewModel : BaseViewModel
{
    public CategoryDto Category { get; }
    public int ProductCount { get; }
    public AsyncCommand OpenCommand { get; }
    public CategoryItemViewModel(CategoryDto category, int count, IAppNavigation navigation)
    {
        Category = category;
        ProductCount = count;
        OpenCommand = Command(() => navigation.OpenCategoryAsync(Category));
    }
}

public sealed class CategoriesViewModel : BaseViewModel
{
    public ObservableCollection<CategoryItemViewModel> Categories { get; } = [];
    public AsyncCommand LoadCommand { get; }
    public AsyncCommand LogoutCommand { get; }
    public CategoriesViewModel(IShopApi api, LocalStore store, IAppNavigation navigation)
    {
        LogoutCommand = Command(navigation.LogoutAsync);
        LoadCommand = Command(async () =>
        {
            List<CategoryDto> categories;
            List<ProductDto> products;
            try
            {
                categories = await api.GetCategoriesAsync();
                products = await api.GetProductsAsync();
                await store.SaveCatalogAsync(products);
                Message = $"Catálogo atualizado · {products.Count} produtos";
            }
            catch
            {
                products = (await store.GetCatalogAsync()).Products;
                categories = products.GroupBy(x => new { x.CategoryId, x.CategoryName })
                    .Select(g => new CategoryDto(g.Key.CategoryId, g.Key.CategoryName, null, true)).ToList();
                Message = products.Count == 0 ? "Sem conexão e sem catálogo salvo." : "Modo offline · dados salvos";
            }
            Categories.Clear();
            foreach (var category in categories.Where(x => x.IsActive))
                Categories.Add(new(category, products.Count(x => x.CategoryId == category.Id && x.IsActive), navigation));
        });
    }
}

public sealed class CatalogViewModel : BaseViewModel
{
    private readonly IAppNavigation navigation;
    private List<ProductDto> products = [];
    private List<CategoryDto> categories = [];
    private string search = "";
    private int selectedCategoryIndex;
    public Guid? CategoryId { get; private set; }
    public string Title { get; private set; } = "Produtos";
    public bool ShowCategoryFilter => CategoryId is null;
    public ObservableCollection<string> CategoryNames { get; } = ["Todas as categorias"];
    public ObservableCollection<ProductItemViewModel> Products { get; } = [];
    public string Search { get => search; set { if (SetProperty(ref search, value)) Filter(); } }
    public int SelectedCategoryIndex { get => selectedCategoryIndex; set { if (SetProperty(ref selectedCategoryIndex, value)) Filter(); } }
    public AsyncCommand LoadCommand { get; }
    public AsyncCommand OpenCartCommand { get; }

    public CatalogViewModel(IShopApi api, LocalStore store, IAppNavigation navigation)
    {
        this.navigation = navigation;
        OpenCartCommand = Command(navigation.OpenCartAsync);
        LoadCommand = Command(async () =>
        {
            // Sempre atualiza o catálogo completo; uma categoria nunca substitui o cache global.
            try
            {
                products = await api.GetProductsAsync();
                categories = await api.GetCategoriesAsync();
                await store.SaveCatalogAsync(products);
                Message = $"Atualizado agora · {products.Count} produtos";
            }
            catch
            {
                var cache = await store.GetCatalogAsync();
                products = cache.Products;
                categories = products.GroupBy(x => new { x.CategoryId, x.CategoryName })
                    .Select(g => new CategoryDto(g.Key.CategoryId, g.Key.CategoryName, null, true)).ToList();
                Message = cache.UpdatedAt is null ? "Sem conexão e sem catálogo salvo." : $"Modo offline · salvo em {cache.UpdatedAt.Value.LocalDateTime:g}";
            }
            CategoryNames.Clear();
            CategoryNames.Add("Todas as categorias");
            foreach (var category in categories) CategoryNames.Add(category.Name);
            SelectedCategoryIndex = 0;
            Filter();
        });
    }
    public void SetCategory(CategoryDto category)
    {
        CategoryId = category.Id;
        Title = category.Name;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(ShowCategoryFilter));
        Filter();
    }
    private void Filter()
    {
        var selectedId = CategoryId ?? (SelectedCategoryIndex > 0 && SelectedCategoryIndex <= categories.Count
            ? categories[SelectedCategoryIndex - 1].Id : (Guid?)null);
        var term = Search?.Trim();
        Products.Clear();
        foreach (var product in products.Where(x => x.IsActive && (selectedId is null || x.CategoryId == selectedId))
            .Where(x => string.IsNullOrWhiteSpace(term) || x.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                || x.Brand.Contains(term, StringComparison.CurrentCultureIgnoreCase) || x.Sku.Contains(term, StringComparison.OrdinalIgnoreCase)))
            Products.Add(new(product, navigation));
    }
}

public sealed class ProductItemViewModel : BaseViewModel
{
    public ProductDto Product { get; }
    public bool InStock => Product.StockAvailable > 0;
    public string ButtonText => InStock ? "Ver e adicionar" : "Produto esgotado";
    public AsyncCommand OpenCommand { get; }
    public ProductItemViewModel(ProductDto product, IAppNavigation navigation)
    {
        Product = product;
        OpenCommand = Command(() => navigation.OpenProductAsync(product), () => InStock);
    }
}

public sealed class ProductViewModel : BaseViewModel
{
    private ProductDto? product;
    private int quantity = 1;
    public ProductDto? Product { get => product; private set => SetProperty(ref product, value); }
    public int Quantity { get => quantity; private set => SetProperty(ref quantity, value); }
    public AsyncCommand IncreaseCommand { get; }
    public AsyncCommand DecreaseCommand { get; }
    public AsyncCommand AddCommand { get; }
    public ProductViewModel(LocalStore store, IUserSession session)
    {
        IncreaseCommand = Command(() => { Quantity = Math.Min(Product!.StockAvailable, Quantity + 1); return Task.CompletedTask; }, () => Product is not null && Quantity < Product.StockAvailable);
        DecreaseCommand = Command(() => { Quantity--; return Task.CompletedTask; }, () => Quantity > 1);
        AddCommand = Command(async () =>
        {
            var id = await session.GetUserIdAsync() ?? throw new InvalidOperationException("Faça login novamente.");
            var cart = await store.GetCartAsync(id);
            var existing = cart.FirstOrDefault(x => x.ProductId == Product!.Id.ToString())?.Quantity ?? 0;
            if (existing + Quantity > Product!.StockAvailable) throw new InvalidOperationException("A quantidade no carrinho ultrapassa o estoque disponível.");
            await store.AddCartAsync(id, Product!, Quantity);
            Message = "✓ Produto adicionado ao carrinho.";
        }, () => Product?.StockAvailable > 0);
    }
    public void SetProduct(ProductDto value)
    {
        Product = value;
        Quantity = value.StockAvailable > 0 ? 1 : 0;
        IncreaseCommand.NotifyCanExecuteChanged();
        DecreaseCommand.NotifyCanExecuteChanged();
        AddCommand.NotifyCanExecuteChanged();
    }
}

using LimpaFacil.Mobile.Ui;
using LimpaFacil.Mobile.ViewModels;

namespace LimpaFacil.Mobile.Pages;

/// <summary>
/// Página inicial com navegação rápida no topo, facilitando o acesso em emuladores
/// onde a barra de abas inferior é difícil de alcançar.
/// </summary>
public sealed class HomePage : ContentPage
{
    public HomePage(CategoriesViewModel categories, CatalogViewModel catalog, MyOrdersViewModel orders, CartViewModel cart)
    {
        Title = "Início";
        BackgroundColor = Theme.Background;

        var heading = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = "LimpaFácil", FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Theme.Green },
                new Label { Text = "O que você precisa hoje?", FontSize = 13, TextColor = Theme.Muted }
            }
        };

        // Botões grandes no topo para acesso rápido (emulador: mais fácil de tocar).
        var nav = new Grid
        {
            ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)],
            RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto)],
            ColumnSpacing = 10,
            RowSpacing = 10
        };
        nav.Add(NavButton("Categorias", "📁", async () => await SwitchToTab(0)), 0, 0);
        nav.Add(NavButton("Produtos", "🧼", async () => await SwitchToTab(1)), 1, 0);
        nav.Add(NavButton("Carrinho", "🛒", async () => await SwitchToTab(3)), 0, 1);
        nav.Add(NavButton("Meus pedidos", "📦", async () => await SwitchToTab(2)), 1, 1);

        var quick = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = "Ações rápidas", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text },
                BoundUi.Button("Atualizar carteira", nameof(CategoriesViewModel.LoadCommand)),
                BoundUi.Button("Abrir carrinho", nameof(CartViewModel.LoadCommand), secondary: true)
            }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 16,
                Children = { heading, nav, quick, BoundUi.Text(nameof(CategoriesViewModel.Message), 12, Theme.Muted) }
            }
        };
        BindingContext = categories;
    }

    private static Button NavButton(string text, string icon, Func<Task> action)
    {
        var btn = new Button
        {
            Text = $"{icon}\n{text}",
            BackgroundColor = Theme.Surface,
            TextColor = Theme.Text,
            FontAttributes = FontAttributes.Bold,
            FontSize = 13,
            CornerRadius = 12,
            HeightRequest = 86,
            BorderColor = Theme.Border,
            BorderWidth = 1
        };
        btn.Clicked += async (_, _) => await action();
        return btn;
    }

    private async Task SwitchToTab(int index)
    {
        await Task.Yield();
        if (Application.Current?.Windows[0].Page is TabbedPage tabs && index < tabs.Children.Count)
        {
            tabs.CurrentPage = tabs.Children[index];
        }
    }
}

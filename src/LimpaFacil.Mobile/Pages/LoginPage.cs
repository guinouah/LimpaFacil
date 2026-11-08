using LimpaFacil.Mobile.ViewModels;
using LimpaFacil.Mobile.Ui;

namespace LimpaFacil.Mobile.Pages;

public sealed class LoginPage : ContentPage
{
    private readonly Entry email = Theme.Entry("seu@email.com", Keyboard.Email);
    private readonly Entry password = Theme.Entry("Sua senha de acesso", password: true);
    private readonly Label message = new() { TextColor = Color.FromArgb("#DC2626"), FontSize = 12 };
    private readonly Button login = Theme.PrimaryButton("Entrar");

    public LoginPage(LoginViewModel viewModel)
    {
        BindingContext = viewModel;
        Title = "Login";
        BackgroundColor = Theme.Background;
        Shell.SetNavBarIsVisible(this, false);
        email.SetBinding(Entry.TextProperty, nameof(LoginViewModel.Email), BindingMode.TwoWay);
        password.SetBinding(Entry.TextProperty, nameof(LoginViewModel.Password), BindingMode.TwoWay);
        message.SetBinding(Label.TextProperty, nameof(LoginViewModel.Message));
        login.SetBinding(Button.CommandProperty, nameof(LoginViewModel.LoginCommand));
        var register = Theme.SecondaryButton("Criar minha conta");
        register.SetBinding(Button.CommandProperty, nameof(LoginViewModel.RegisterCommand));

        Content = new ScrollView
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = new VerticalStackLayout
            {
                WidthRequest = 350, MaximumWidthRequest = 380, HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center, Padding = new Thickness(18, 24), Spacing = 14,
                Children =
                {
                    new Image { Source = "mascote_avatar.png", WidthRequest = 105, HeightRequest = 105, Aspect = Aspect.AspectFill, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "LimpaFácil", FontSize = 29, FontAttributes = FontAttributes.Bold, TextColor = Theme.Blue, HorizontalTextAlignment = TextAlignment.Center },
                    new Label { Text = "Produtos de limpeza para retirada rápida", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Theme.Cyan, HorizontalTextAlignment = TextAlignment.Center },
                    Theme.Card(new VerticalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            new Label { Text = "Bem-vindo(a)! 👋", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text },
                            new Label { Text = "Acesse sua conta para continuar", FontSize = 12, TextColor = Theme.Muted },
                            new Label { Text = "E-mail", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Theme.Text },
                            Theme.Field(email, "✉"),
                            new Label { Text = "Senha", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Theme.Text },
                            Theme.Field(password, "🔒"),
                            message,
                            login
                        }
                    }),
                    register,
                    new Label { Text = "⚡ Retirada expressa     🛡 Compra segura", FontSize = 11, TextColor = Theme.Muted, HorizontalTextAlignment = TextAlignment.Center }
                }
            }
        };
    }

}

using LimpaFacil.Mobile.ViewModels;
using LimpaFacil.Mobile.Ui;

namespace LimpaFacil.Mobile.Pages;

public sealed class RegistrationPage : ContentPage
{
    private readonly Picker customerType = new()
    {
        Title = "Tipo de cadastro", ItemsSource = new[] { "Pessoa física", "Pessoa jurídica" }, SelectedIndex = 0,
        BackgroundColor = Colors.White, TextColor = Theme.Text, TitleColor = Theme.Muted
    };
    private readonly Entry name = Theme.Entry("Nome completo");
    private readonly Entry taxId = Theme.Entry("CPF", Keyboard.Numeric);
    private readonly Entry email = Theme.Entry("seu@email.com", Keyboard.Email);
    private readonly Entry password = Theme.Entry("Mínimo 10 caracteres", password: true);
    private readonly Label message = new() { TextColor = Color.FromArgb("#DC2626"), FontSize = 12 };
    private readonly Button register = Theme.PrimaryButton("Cadastrar e continuar");

    public RegistrationPage(RegistrationViewModel viewModel)
    {
        BindingContext = viewModel;
        Title = "Criar conta";
        BackgroundColor = Theme.Background;
        customerType.SetBinding(Picker.SelectedIndexProperty, nameof(RegistrationViewModel.CustomerTypeIndex), BindingMode.TwoWay);
        name.SetBinding(Entry.TextProperty, nameof(RegistrationViewModel.Name), BindingMode.TwoWay);
        name.SetBinding(Entry.PlaceholderProperty, nameof(RegistrationViewModel.NamePlaceholder));
        taxId.SetBinding(Entry.TextProperty, nameof(RegistrationViewModel.TaxId), BindingMode.TwoWay);
        taxId.SetBinding(Entry.PlaceholderProperty, nameof(RegistrationViewModel.TaxIdPlaceholder));
        email.SetBinding(Entry.TextProperty, nameof(RegistrationViewModel.Email), BindingMode.TwoWay);
        password.SetBinding(Entry.TextProperty, nameof(RegistrationViewModel.Password), BindingMode.TwoWay);
        message.SetBinding(Label.TextProperty, nameof(RegistrationViewModel.Message));
        register.SetBinding(Button.CommandProperty, nameof(RegistrationViewModel.RegisterCommand));
        var back = Theme.SecondaryButton("Já tenho conta");
        back.SetBinding(Button.CommandProperty, nameof(RegistrationViewModel.LoginCommand));

        Content = new ScrollView
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = new VerticalStackLayout
            {
                WidthRequest = 360, MaximumWidthRequest = 390, HorizontalOptions = LayoutOptions.Center,
                Padding = 20, Spacing = 14,
                Children =
                {
                    new Image { Source = "mascote_avatar.png", WidthRequest = 88, HeightRequest = 88, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "Crie sua conta", FontSize = 27, FontAttributes = FontAttributes.Bold, TextColor = Theme.Blue, HorizontalTextAlignment = TextAlignment.Center },
                    new Label { Text = "Cadastre-se para montar e retirar seus pedidos", FontSize = 12, TextColor = Theme.Muted, HorizontalTextAlignment = TextAlignment.Center },
                    Theme.Card(new VerticalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            new Label { Text = "Tipo de cliente", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text }, customerType,
                            new Label { Text = "Nome *", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text }, Theme.Field(name, "👤"),
                            new Label { Text = "CPF ou CNPJ *", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text }, Theme.Field(taxId, "#"),
                            new Label { Text = "E-mail *", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text }, Theme.Field(email, "✉"),
                            new Label { Text = "Senha *", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text }, Theme.Field(password, "🔒"),
                            message, register
                        }
                    }),
                    back
                }
            }
        };
    }

}

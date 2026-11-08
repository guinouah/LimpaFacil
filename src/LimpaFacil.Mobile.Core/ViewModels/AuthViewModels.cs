using LimpaFacil.Domain;
using LimpaFacil.Mobile.Mvvm;
using LimpaFacil.Mobile.Services;

namespace LimpaFacil.Mobile.ViewModels;

public sealed class LoginViewModel : BaseViewModel
{
    private string email = "", password = "";
    public string Email { get => email; set => SetProperty(ref email, value); }
    public string Password { get => password; set => SetProperty(ref password, value); }
    public AsyncCommand LoginCommand { get; }
    public AsyncCommand RegisterCommand { get; }
    public LoginViewModel(IShopApi api, IAppNavigation navigation)
    {
        LoginCommand = Command(async () =>
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
                throw new InvalidOperationException("Informe seu e-mail e sua senha.");
            Message = "Entrando...";
            await api.LoginAsync(Email.Trim(), Password);
            Password = "";
            navigation.ShowShop();
        });
        RegisterCommand = Command(() => { navigation.ShowRegistration(); return Task.CompletedTask; });
    }
}
public sealed class RegistrationViewModel : BaseViewModel
{
    private string name = "", taxId = "", email = "", password = "";
    private int customerTypeIndex;
    public string Name { get => name; set => SetProperty(ref name, value); }
    public string TaxId { get => taxId; set => SetProperty(ref taxId, value); }
    public string Email { get => email; set => SetProperty(ref email, value); }
    public string Password { get => password; set => SetProperty(ref password, value); }
    public int CustomerTypeIndex
    {
        get => customerTypeIndex;
        set
        {
            if (!SetProperty(ref customerTypeIndex, value)) return;
            OnPropertyChanged(nameof(NamePlaceholder));
            OnPropertyChanged(nameof(TaxIdPlaceholder));
        }
    }
    public string NamePlaceholder => CustomerTypeIndex == 1 ? "Razão social" : "Nome completo";
    public string TaxIdPlaceholder => CustomerTypeIndex == 1 ? "CNPJ" : "CPF";
    public AsyncCommand RegisterCommand { get; }
    public AsyncCommand LoginCommand { get; }
    public RegistrationViewModel(IShopApi api, IAppNavigation navigation)
    {
        RegisterCommand = Command(async () =>
        {
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(TaxId))
                throw new InvalidOperationException("Preencha nome, documento e e-mail.");
            if (Password.Length < 10) throw new InvalidOperationException("A senha deve ter pelo menos 10 caracteres.");
            Message = "Criando conta...";
            await api.RegisterAsync(Name.Trim(), Email.Trim(), Password,
                CustomerTypeIndex == 1 ? CustomerType.Company : CustomerType.Individual, TaxId.Trim());
            Password = "";
            navigation.ShowShop();
        });
        LoginCommand = Command(() => { navigation.ShowLogin(); return Task.CompletedTask; });
    }
}
public sealed class StartupViewModel : BaseViewModel
{
    private bool initialized;
    public AsyncCommand InitializeCommand { get; }
    public StartupViewModel(IAppNavigation navigation)
    {
        Message = "Abrindo LimpaFácil...";
        InitializeCommand = Command(async () =>
        {
            if (initialized) return;
            await navigation.InitializeAsync();
            initialized = true;
        });
    }
}

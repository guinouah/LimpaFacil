namespace LimpaFacil.Mobile.Services;

public sealed class NetworkStatus : INetworkStatus
{
    public bool IsOnline => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
}

public sealed class UserDialogs : IUserDialogs
{
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        Page page = Application.Current!.Windows[0].Page!;
        if (page is TabbedPage tabs) page = tabs.CurrentPage!;
        if (page is NavigationPage navigation) page = navigation.CurrentPage;
        return page.DisplayAlertAsync(title, message, accept, cancel);
    }
}

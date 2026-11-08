using LimpaFacil.Contracts;

namespace LimpaFacil.Mobile.Services;

public sealed class TokenStore : IUserSession
{
    private const string Access = "auth_access"; private const string Refresh = "auth_refresh";
    private const string UserId = "auth_user_id"; private const string UserName = "auth_user_name";
    public Task<string?> GetAccessAsync() => SecureStorage.Default.GetAsync(Access);
    public Task<string?> GetRefreshAsync() => SecureStorage.Default.GetAsync(Refresh);
    public async Task<Guid?> GetUserIdAsync() => Guid.TryParse(await SecureStorage.Default.GetAsync(UserId), out var id) ? id : null;
    public Task<string?> GetUserNameAsync() => SecureStorage.Default.GetAsync(UserName);
    public async Task SaveAsync(AuthResponse auth)
    {
        await SecureStorage.Default.SetAsync(Access, auth.AccessToken); await SecureStorage.Default.SetAsync(Refresh, auth.RefreshToken);
        await SecureStorage.Default.SetAsync(UserId, auth.User.Id.ToString()); await SecureStorage.Default.SetAsync(UserName, auth.User.Name);
    }
    public void Clear() { SecureStorage.Default.Remove(Access); SecureStorage.Default.Remove(Refresh); SecureStorage.Default.Remove(UserId); SecureStorage.Default.Remove(UserName); }
}

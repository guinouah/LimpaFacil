using System.Security.Claims;

namespace LimpaFacil.Api.Services;

public sealed class CurrentUser(IHttpContextAccessor accessor)
{
    public Guid Id => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? accessor.HttpContext?.User.FindFirstValue("sub"), out var id)
        ? id : throw new UnauthorizedAccessException("Usuário não autenticado.");
}

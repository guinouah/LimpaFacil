using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LimpaFacil.Api.Data;
using LimpaFacil.Contracts;
using LimpaFacil.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LimpaFacil.Api.Services;

public sealed class TokenService(IConfiguration config, AppDbContext db)
{
    public async Task<AuthResponse> IssueAsync(User user, string? device, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(config.GetValue("Jwt:AccessTokenMinutes", 30));
        var key = GetKey();
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var jwt = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"], audience: config["Jwt:Audience"], claims: claims,
            notBefore: now.UtcDateTime, expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        db.RefreshSessions.Add(new RefreshSession
        {
            UserId = user.Id,
            TokenHash = Hash(refreshToken),
            Device = string.IsNullOrWhiteSpace(device) ? null : device[..Math.Min(device.Length, 200)],
            ExpiresAt = now.AddDays(config.GetValue("Jwt:RefreshTokenDays", 14))
        });
        await db.SaveChangesAsync(ct);
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(jwt), refreshToken, expires,
            new UserSummary(user.Id, user.Name, user.Email, user.Role));
    }

    public async Task<AuthResponse?> RefreshAsync(string token, string? device, CancellationToken ct)
    {
        var hash = Hash(token);
        var session = await db.RefreshSessions.Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (session is null || session.RevokedAt is not null || session.ExpiresAt <= DateTimeOffset.UtcNow || !session.User.IsActive)
            return null;
        session.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return await IssueAsync(session.User, device, ct);
    }

    public async Task RevokeAsync(string token, CancellationToken ct)
    {
        var hash = Hash(token);
        var session = await db.RefreshSessions.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (session is not null && session.RevokedAt is null)
        {
            session.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public SymmetricSecurityKey GetKey()
    {
        var value = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key não configurada.");
        if (Encoding.UTF8.GetByteCount(value) < 32) throw new InvalidOperationException("Jwt:Key deve ter ao menos 32 bytes.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(value));
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

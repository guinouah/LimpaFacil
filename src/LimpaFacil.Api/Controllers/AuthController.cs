using System.ComponentModel.DataAnnotations;
using LimpaFacil.Api.Data;
using LimpaFacil.Api.Services;
using LimpaFacil.Contracts;
using LimpaFacil.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LimpaFacil.Api.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(AppDbContext db, IPasswordHasher<User> hasher, TokenService tokens) : ControllerBase
{
    [HttpPost("register"), AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var taxId = new string((request.TaxId ?? string.Empty).Where(char.IsDigit).ToArray());
        if (name.Length is < 2 or > 120) return ValidationProblem("O nome deve ter entre 2 e 120 caracteres.");
        if (!new EmailAddressAttribute().IsValid(email)) return ValidationProblem("E-mail inválido.");
        if (request.Password.Length < 10) return ValidationProblem("A senha deve ter ao menos 10 caracteres.");
        if (!Enum.IsDefined(request.CustomerType)) return ValidationProblem("Tipo de cliente inválido.");
        var expectedTaxIdLength = request.CustomerType == CustomerType.Individual ? 11 : 14;
        if (taxId.Length != expectedTaxIdLength)
            return ValidationProblem(request.CustomerType == CustomerType.Individual ? "CPF deve conter 11 números." : "CNPJ deve conter 14 números.");
        if (await db.Users.AnyAsync(x => x.Email == email, ct)) return Conflict(new ApiError("email_in_use", "Este e-mail já está cadastrado."));
        if (await db.Users.AnyAsync(x => x.TaxId == taxId, ct)) return Conflict(new ApiError("document_in_use", "Este CPF/CNPJ já está cadastrado."));
        var user = new User { Name = name, Email = email, CustomerType = request.CustomerType, TaxId = taxId, Role = UserRole.Client };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return Ok(await tokens.IssueAsync(user, null, ct));
    }

    [HttpPost("login"), AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, ct);
        if (user is null || !user.IsActive || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new ApiError("invalid_credentials", "E-mail ou senha inválidos."));
        return Ok(await tokens.IssueAsync(user, request.Device, ct));
    }

    [HttpPost("refresh"), AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var response = await tokens.RefreshAsync(request.RefreshToken, request.Device, ct);
        return response is null ? Unauthorized(new ApiError("invalid_refresh_token", "Sessão expirada ou inválida.")) : Ok(response);
    }

    [HttpPost("logout"), AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        await tokens.RevokeAsync(request.RefreshToken, ct);
        return NoContent();
    }
}

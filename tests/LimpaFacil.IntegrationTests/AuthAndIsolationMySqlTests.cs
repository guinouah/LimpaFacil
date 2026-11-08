using System.Security.Claims;
using LimpaFacil.Api.Controllers;
using LimpaFacil.Api.Services;
using LimpaFacil.Contracts;
using LimpaFacil.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LimpaFacil.IntegrationTests;

public sealed class AuthAndIsolationMySqlTests
{
    [MySqlFact]
    public async Task Public_registration_creates_only_client_and_login_validates_password()
    {
        await using var server = await MySqlTestDatabase.CreateAsync(); await using var db = server.CreateContext();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-key-with-at-least-32-characters-123456", ["Jwt:Issuer"] = "tests", ["Jwt:Audience"] = "tests"
        }).Build();
        var hasher = new PasswordHasher<User>(); var controller = new AuthController(db, hasher, new TokenService(config, db));
        var registered = await controller.Register(new("Cliente", "client@example.test", "senha-segura-123", CustomerType.Individual, "12345678909"), default);
        Assert.IsType<OkObjectResult>(registered.Result);
        var saved = await db.Users.SingleAsync(); Assert.Equal(UserRole.Client, saved.Role);
        Assert.IsType<UnauthorizedObjectResult>((await controller.Login(new("client@example.test", "errada"), default)).Result);
        Assert.IsType<OkObjectResult>((await controller.Login(new("client@example.test", "senha-segura-123"), default)).Result);
    }

    [MySqlFact]
    public async Task Client_cannot_read_another_clients_order()
    {
        await using var server = await MySqlTestDatabase.CreateAsync(); await using var db = server.CreateContext();
        var a = new User { Name = "A", Email = "a@example.test", PasswordHash = "x" }; var b = new User { Name = "B", Email = "b@example.test", PasswordHash = "x" };
        var order = new Order { Client = b, Number = "LF-OTHER", IdempotencyKey = "other", Total = 1m };
        db.AddRange(a, b, order); await db.SaveChangesAsync();
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, a.Id.ToString())], "test")) };
        var accessor = new HttpContextAccessor { HttpContext = context };
        var controller = new OrdersController(db, new CurrentUser(accessor), new OrderService(db));
        var result = await controller.Get(order.Id, default);
        Assert.IsType<NotFoundResult>(result.Result);
    }
}

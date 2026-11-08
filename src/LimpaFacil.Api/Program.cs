using System.Security.Claims;
using LimpaFacil.Api.Data;
using LimpaFacil.Api.Middleware;
using LimpaFacil.Api.Services;
using LimpaFacil.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
var connectionString = builder.Configuration.GetConnectionString("MySql")
    ?? throw new InvalidOperationException("ConnectionStrings:MySql não configurada.");

// Checkout e estoque controlam transações explícitas; a tentativa lógica é protegida por idempotência.
builder.Services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString,
    new MySqlServerVersion(new Version(8, 4, 0))));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddControllers();
if (builder.Environment.IsDevelopment())
{
    var keysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
    Directory.CreateDirectory(keysPath);
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "LimpaFácil API", Version = "v1", Description = "API central da loja LimpaFácil." });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});
builder.Services.AddCors(options => options.AddPolicy("clients", policy =>
{
    var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length > 0) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));

var jwtKey = builder.Configuration["Jwt:Key"] ?? string.Empty;
if (System.Text.Encoding.UTF8.GetByteCount(jwtKey) < 32 || jwtKey.StartsWith("ALTERE", StringComparison.OrdinalIgnoreCase) || jwtKey.StartsWith("SUBSTITUA", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Jwt:Key deve ter ao menos 32 bytes e ser definida fora do código em produção.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var idValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Principal?.FindFirstValue("sub");
            if (!Guid.TryParse(idValue, out var id)) { context.Fail("Sessão inválida."); return; }
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            if (!await db.Users.AsNoTracking().AnyAsync(x => x.Id == id && x.IsActive, context.HttpContext.RequestAborted))
                context.Fail("Conta desativada.");
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey)),
        NameClaimType = ClaimTypes.Name, RoleClaimType = ClaimTypes.Role, ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();
var localMySql = builder.Environment.IsDevelopment()
    ? await LocalMySqlServer.StartIfAvailableAsync(app.Environment.ContentRootPath, connectionString)
    : null;
if (localMySql is not null) app.Lifetime.ApplicationStopping.Register(localMySql.Dispose);

app.UseMiddleware<ExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("clients");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow })).AllowAnonymous();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup") || args.Contains("--create-admin"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    if (app.Configuration.GetValue<bool>("Database:SeedDemoData")) await seeder.SeedDemoAsync();
    if (args.Contains("--create-admin"))
    {
        await seeder.CreateFirstAdminFromEnvironmentAsync();
        Console.WriteLine("Primeiro administrador criado com sucesso.");
        return;
    }
}

await app.RunAsync();

public partial class Program { }

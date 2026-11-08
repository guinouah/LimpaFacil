using LimpaFacil.Api.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace LimpaFacil.IntegrationTests;

public sealed class MySqlTestDatabase : IAsyncDisposable
{
    private readonly string adminConnection; private readonly string databaseName;
    public string ConnectionString { get; }
    private MySqlTestDatabase(string adminConnection, string databaseName, string connectionString) =>
        (this.adminConnection, this.databaseName, ConnectionString) = (adminConnection, databaseName, connectionString);
    public static async Task<MySqlTestDatabase> CreateAsync()
    {
        var configured = Environment.GetEnvironmentVariable("LIMPAFACIL_TEST_MYSQL")!;
        var admin = new MySqlConnectionStringBuilder(configured) { Database = "" };
        // O provedor acrescenta um sufixo ao nome no GET_LOCK (limite MySQL: 64 caracteres).
        var name = $"lf_test_{Guid.NewGuid():N}";
        await using (var conn = new MySqlConnection(admin.ConnectionString)) { await conn.OpenAsync(); await new MySqlCommand($"CREATE DATABASE `{name}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci", conn).ExecuteNonQueryAsync(); }
        var app = new MySqlConnectionStringBuilder(configured) { Database = name };
        var instance = new MySqlTestDatabase(admin.ConnectionString, name, app.ConnectionString);
        try { await using var db = instance.CreateContext(); await db.Database.MigrateAsync(); return instance; }
        catch { await instance.DisposeAsync(); throw; }
    }
    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseMySql(ConnectionString, new MySqlServerVersion(new Version(8, 4, 0))).Options;
        return new AppDbContext(options);
    }
    public async ValueTask DisposeAsync()
    {
        await using var conn = new MySqlConnection(adminConnection); await conn.OpenAsync();
        await new MySqlCommand($"DROP DATABASE IF EXISTS `{databaseName}`", conn).ExecuteNonQueryAsync();
    }
}

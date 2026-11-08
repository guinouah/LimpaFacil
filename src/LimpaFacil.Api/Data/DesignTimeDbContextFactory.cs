using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LimpaFacil.Api.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        var connection = Environment.GetEnvironmentVariable("LIMPAFACIL_MYSQL")
            ?? "Server=localhost;Port=3306;Database=limpafacil;User=limpafacil_app;Password=design_time_only";
        builder.UseMySql(connection, new MySqlServerVersion(new Version(8, 4, 0)));
        return new AppDbContext(builder.Options);
    }
}

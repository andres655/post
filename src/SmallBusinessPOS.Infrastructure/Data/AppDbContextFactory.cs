using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SmallBusinessPOS.Infrastructure.Data;

/// <summary>
/// Fábrica usada por las herramientas de EF Core (migrations, scaffolding) en tiempo de diseño.
/// Solo para desarrollo — no expone ninguna cadena de conexión de producción.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=SmallBusinessPOSDb_Dev;Trusted_Connection=True;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions => sqlOptions.MigrationsAssembly("SmallBusinessPOS.Infrastructure"));

        return new AppDbContext(optionsBuilder.Options);
    }
}

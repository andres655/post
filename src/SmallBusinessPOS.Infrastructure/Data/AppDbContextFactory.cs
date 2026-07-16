using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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

        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=SmallBusinessPOSDb_Dev;Trusted_Connection=True;TrustServerCertificate=True;",
            sqlOptions => sqlOptions.MigrationsAssembly("SmallBusinessPOS.Infrastructure"));

        return new AppDbContext(optionsBuilder.Options);
    }
}

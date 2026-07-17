using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Infrastructure.Data;
using SmallBusinessPOS.Infrastructure.Data.Identity;
using SmallBusinessPOS.Infrastructure.Data.Seed;
using SmallBusinessPOS.Infrastructure.Services;

namespace SmallBusinessPOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión 'DefaultConnection'. " +
                "Configúrela en appsettings.json o en secretos de usuario.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("SmallBusinessPOS.Infrastructure");
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                }));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<DataSeeder>();
        services.AddScoped<ISynchronizationService, LocalOnlySynchronizationService>();
        services.AddScoped<IReceiptService, QuestPdfReceiptService>();
        services.AddScoped<IReportExportService, ReportExportService>();

        return services;
    }
}

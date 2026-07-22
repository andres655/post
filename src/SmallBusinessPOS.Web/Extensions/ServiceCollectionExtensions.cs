using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;
using SmallBusinessPOS.Application;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Infrastructure;
using SmallBusinessPOS.Web.Services.Pages;
using SmallBusinessPOS.Web.Services;

namespace SmallBusinessPOS.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            var keysPath = configuration["DataProtection:KeysPath"];
            if (string.IsNullOrWhiteSpace(keysPath))
            {
                keysPath = Path.Combine(environment.ContentRootPath, ".app-data", "data-protection-keys");
            }

            Directory.CreateDirectory(keysPath);

            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
                .SetApplicationName("SmallBusinessPOS");
        }

        services.AddInfrastructureServices(configuration);
        services.AddApplicationServices();
        services.AddCascadingAuthenticationState();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorageService, WebRootFileStorageService>();
        services.AddScoped<ProductionPageService>();

        services.AddRazorComponents()
            .AddInteractiveServerComponents(options =>
            {
                options.DetailedErrors = environment.IsDevelopment();
            });

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.AccessDeniedPath = "/access-denied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events.OnValidatePrincipal = async context =>
            {
                if (context.Principal is null)
                    return;

                var userId = context.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                    return;

                var authentication = context.HttpContext.RequestServices.GetRequiredService<IUserAuthenticationService>();
                if (!await authentication.IsActiveUserAsync(userId))
                {
                    context.RejectPrincipal();
                    await authentication.SignOutAsync();
                }
            };
        });

        return services;
    }
}

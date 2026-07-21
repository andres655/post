using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using SmallBusinessPOS.Application;
using SmallBusinessPOS.Infrastructure;
using SmallBusinessPOS.Infrastructure.Data.Identity;

namespace SmallBusinessPOS.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddInfrastructureServices(configuration);
        services.AddApplicationServices();
        services.AddCascadingAuthenticationState();

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

                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var userId = userManager.GetUserId(context.Principal);
                if (string.IsNullOrWhiteSpace(userId))
                    return;

                var user = await userManager.FindByIdAsync(userId);
                if (user is null || !user.IsActive)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                }
            };
        });

        return services;
    }
}

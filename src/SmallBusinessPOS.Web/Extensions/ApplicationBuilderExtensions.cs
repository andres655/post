using SmallBusinessPOS.Infrastructure.Data.Seed;

namespace SmallBusinessPOS.Web.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task SeedDataIfEnabledAsync(this WebApplication app)
    {
        var seedOnStartup = app.Environment.IsDevelopment()
            || app.Configuration.GetValue<bool>("SeedData:RunOnStartup");

        if (!seedOnStartup)
            return;

        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var adminPwd = config["SeedData:AdminPassword"]
            ?? throw new InvalidOperationException("SeedData:AdminPassword no configurado.");
        var supervisorPwd = config["SeedData:SupervisorPassword"]
            ?? throw new InvalidOperationException("SeedData:SupervisorPassword no configurado.");
        var cashierPwd = config["SeedData:CashierPassword"]
            ?? throw new InvalidOperationException("SeedData:CashierPassword no configurado.");

        await seeder.SeedAsync(adminPwd, supervisorPwd, cashierPwd);
    }

    public static void UseConfiguredSecurityHeaders(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
        }

        var forceHttps = app.Configuration.GetValue<bool>("Hosting:ForceHttps");
        if (!app.Environment.IsDevelopment() && forceHttps)
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }
    }
}

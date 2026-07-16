using Microsoft.AspNetCore.Identity;
using Serilog;
using SmallBusinessPOS.Application;
using SmallBusinessPOS.Infrastructure;
using SmallBusinessPOS.Infrastructure.Data.Identity;
using SmallBusinessPOS.Infrastructure.Data.Seed;
using SmallBusinessPOS.Web.Components;
using SmallBusinessPOS.Application.Interfaces;

// Configurar Serilog antes del host para capturar errores de arranque
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog desde appsettings
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    // Servicios de Infrastructure y Application
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddApplicationServices();

    // Razor Components con Interactive Server
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Identity UI (páginas de login/logout integradas)
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

    var app = builder.Build();

    // Ejecutar migraciones y seed en desarrollo
    if (app.Environment.IsDevelopment())
    {
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

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    app.MapStaticAssets();

    app.MapGet("/api/receipts/sale/{saleId:guid}", async (
        Guid saleId,
        IReceiptService receiptService,
        CancellationToken ct) =>
    {
        var bytes = await receiptService.GenerateSaleReceiptAsync(saleId, ct);
        return Results.File(bytes, "application/pdf", $"ticket-{saleId:N}.pdf");
    }).RequireAuthorization();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación terminó inesperadamente durante el arranque.");
}
finally
{
    Log.CloseAndFlush();
}


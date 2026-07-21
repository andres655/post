using Serilog;
using SmallBusinessPOS.Web.Components;
using SmallBusinessPOS.Web.Endpoints;
using SmallBusinessPOS.Web.Extensions;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    builder.Services.AddWebApplicationServices(builder.Configuration, builder.Environment);

    var app = builder.Build();

    await app.SeedDataIfEnabledAsync();

    app.UseConfiguredSecurityHeaders();
    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    app.UseStaticFiles();
    app.MapStaticAssets();

    app.MapAuthEndpoints();
    app.MapReceiptEndpoints();
    app.MapReportEndpoints();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicacion termino inesperadamente durante el arranque.");
}
finally
{
    Log.CloseAndFlush();
}

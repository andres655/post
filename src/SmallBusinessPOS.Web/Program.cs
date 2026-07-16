using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SmallBusinessPOS.Application;
using SmallBusinessPOS.Infrastructure;
using SmallBusinessPOS.Infrastructure.Data.Identity;
using SmallBusinessPOS.Infrastructure.Data.Seed;
using SmallBusinessPOS.Web.Components;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Sales.GetSaleByNumber;
using SmallBusinessPOS.Domain.Entities;
using System.Security.Claims;
using System.Net;

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
    builder.Services.AddCascadingAuthenticationState();

    // Razor Components con Interactive Server
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents(options =>
        {
            options.DetailedErrors = builder.Environment.IsDevelopment();
        });

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

        app.MapGet("/login", (HttpContext httpContext) =>
        {
                var returnUrlRaw = httpContext.Request.Query["ReturnUrl"].ToString();
                var error = httpContext.Request.Query["error"].ToString();

                var returnUrl = string.IsNullOrWhiteSpace(returnUrlRaw) ? "/" : returnUrlRaw;
                var safeReturnUrl = WebUtility.HtmlEncode(returnUrl);
                var safeError = WebUtility.HtmlEncode(error);

                var errorHtml = string.IsNullOrWhiteSpace(error)
                        ? string.Empty
                        : $"<div style='padding:10px;border:1px solid #f5c2c7;background:#f8d7da;color:#842029;border-radius:8px;margin-bottom:12px;'>{safeError}</div>";

                var html =
                        "<!doctype html>" +
                        "<html lang=\"es\">" +
                        "<head>" +
                        "<meta charset=\"utf-8\" />" +
                        "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />" +
                        "<title>Iniciar sesión</title>" +
                        "<style>" +
                        "body { font-family: Segoe UI, sans-serif; background:#f6f7fb; margin:0; }" +
                        ".wrap { min-height:100vh; display:flex; align-items:center; justify-content:center; padding:20px; }" +
                        ".card { width:100%; max-width:420px; background:white; border-radius:12px; box-shadow:0 12px 32px rgba(0,0,0,.08); padding:24px; }" +
                        "h1 { margin:0 0 16px; font-size:1.4rem; }" +
                        "label { display:block; font-weight:600; margin:10px 0 6px; }" +
                        "input[type=text], input[type=password] { width:100%; box-sizing:border-box; padding:10px; border:1px solid #d0d5dd; border-radius:8px; }" +
                        ".row { margin-top:12px; display:flex; align-items:center; gap:8px; }" +
                        "button { width:100%; margin-top:14px; border:0; border-radius:8px; padding:10px 12px; background:#0d6efd; color:white; font-weight:600; cursor:pointer; }" +
                        ".muted { color:#667085; font-size:.9rem; margin-top:12px; }" +
                        "</style>" +
                        "</head>" +
                        "<body>" +
                        "<div class=\"wrap\">" +
                        "<form class=\"card\" method=\"post\" action=\"/login\">" +
                        "<h1>SmallBusinessPOS</h1>" +
                        errorHtml +
                        "<input type=\"hidden\" name=\"returnUrl\" value=\"" + safeReturnUrl + "\" />" +
                        "<label for=\"email\">Usuario (email)</label>" +
                        "<input id=\"email\" name=\"email\" type=\"text\" autocomplete=\"username\" required />" +
                        "<label for=\"password\">Contraseña</label>" +
                        "<input id=\"password\" name=\"password\" type=\"password\" autocomplete=\"current-password\" required />" +
                        "<div class=\"row\">" +
                        "<input id=\"rememberMe\" name=\"rememberMe\" type=\"checkbox\" />" +
                        "<label for=\"rememberMe\" style=\"margin:0;font-weight:500;\">Recordarme</label>" +
                        "</div>" +
                        "<button type=\"submit\">Entrar</button>" +
                        "<div class=\"muted\">Usuarios seed: admin@pollosaboroso.local, supervisor@pollosaboroso.local, cajero@pollosaboroso.local</div>" +
                        "</form>" +
                        "</div>" +
                        "</body>" +
                        "</html>";

                return Results.Content(html, "text/html; charset=utf-8");
        }).AllowAnonymous();

        app.MapPost("/login", async (HttpRequest request, SignInManager<ApplicationUser> signInManager) =>
        {
                var form = await request.ReadFormAsync();
                var email = form["email"].ToString();
                var password = form["password"].ToString();
                var rememberMe = string.Equals(form["rememberMe"].ToString(), "on", StringComparison.OrdinalIgnoreCase);
                var returnUrl = form["returnUrl"].ToString();

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                        return Results.Redirect($"/login?error={WebUtility.UrlEncode("Credenciales requeridas")}&ReturnUrl={WebUtility.UrlEncode(returnUrl)}");

                var result = await signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true);
                if (!result.Succeeded)
                        return Results.Redirect($"/login?error={WebUtility.UrlEncode("Usuario o contraseña inválidos")}&ReturnUrl={WebUtility.UrlEncode(returnUrl)}");

                var isLocal = !string.IsNullOrWhiteSpace(returnUrl)
                        && returnUrl.StartsWith('/')
                        && !returnUrl.StartsWith("//")
                        && !returnUrl.StartsWith("/\\");

                return Results.Redirect(isLocal ? returnUrl : "/");
        }).DisableAntiforgery().AllowAnonymous();

        app.MapGet("/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
                await signInManager.SignOutAsync();
                return Results.Redirect("/login");
        }).AllowAnonymous();

        app.MapGet("/access-denied", () =>
                Results.Content("<h1>Acceso denegado</h1><p>No tienes permisos para acceder a esta página.</p><p><a href='/'>Volver al inicio</a></p>", "text/html; charset=utf-8"))
                .AllowAnonymous();

    app.MapGet("/api/receipts/sale/{saleId:guid}", async (
        Guid saleId,
        IReceiptService receiptService,
        CancellationToken ct) =>
    {
        var bytes = await receiptService.GenerateSaleReceiptAsync(saleId, ct);
        return Results.File(bytes, "application/pdf", $"ticket-{saleId:N}.pdf");
    }).RequireAuthorization(policy => policy.RequireRole("Cashier", "Supervisor", "Administrator"));

    app.MapGet("/api/receipts/sale/by-number/{number}", async (
        string number,
        GetPosContextHandler contextHandler,
        GetSaleByNumberHandler lookupHandler,
        IReceiptService receiptService,
        CancellationToken ct) =>
    {
        var contextResult = await contextHandler.HandleAsync(new GetPosContextQuery(), ct);
        if (contextResult.IsFailure)
            return Results.NotFound(contextResult.Error.Description);

        var lookup = await lookupHandler.HandleAsync(new GetSaleByNumberQuery(
            contextResult.Value.BusinessId,
            number), ct);

        if (lookup.IsFailure)
            return Results.NotFound(lookup.Error.Description);

        var bytes = await receiptService.GenerateSaleReceiptAsync(lookup.Value.SaleId, ct);
        return Results.File(bytes, "application/pdf", $"ticket-{lookup.Value.Number}.pdf");
    }).RequireAuthorization(policy => policy.RequireRole("Supervisor", "Administrator"));

    app.MapGet("/api/receipts/reprint/sale/{saleId:guid}", async (
        Guid saleId,
        ClaimsPrincipal user,
        GetPosContextHandler contextHandler,
        IAppDbContext db,
        IReceiptService receiptService,
        CancellationToken ct) =>
    {
        var contextResult = await contextHandler.HandleAsync(new GetPosContextQuery(), ct);
        if (contextResult.IsFailure)
            return Results.NotFound(contextResult.Error.Description);

        var sale = await db.Sales
            .Where(s => s.Id == saleId && s.BusinessId == contextResult.Value.BusinessId)
            .Select(s => new { s.Id, s.ReceiptNumber })
            .FirstOrDefaultAsync(ct);

        if (sale is null)
            return Results.NotFound("Venta no encontrada.");

        var actor = user.Identity?.Name
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "unknown";

        db.ReceiptReprintAudits.Add(ReceiptReprintAudit.Create(
            contextResult.Value.BusinessId,
            contextResult.Value.BranchId,
            sale.Id,
            sale.ReceiptNumber,
            actor,
            "SaleId"));

        await db.SaveChangesAsync(ct);

        var bytes = await receiptService.GenerateSaleReceiptAsync(sale.Id, ct);
        return Results.File(bytes, "application/pdf", $"ticket-{sale.ReceiptNumber}.pdf");
    }).RequireAuthorization(policy => policy.RequireRole("Supervisor", "Administrator"));

    app.MapGet("/api/receipts/reprint/by-number/{number}", async (
        string number,
        ClaimsPrincipal user,
        GetPosContextHandler contextHandler,
        GetSaleByNumberHandler lookupHandler,
        IAppDbContext db,
        IReceiptService receiptService,
        CancellationToken ct) =>
    {
        var contextResult = await contextHandler.HandleAsync(new GetPosContextQuery(), ct);
        if (contextResult.IsFailure)
            return Results.NotFound(contextResult.Error.Description);

        var lookup = await lookupHandler.HandleAsync(new GetSaleByNumberQuery(
            contextResult.Value.BusinessId,
            number), ct);

        if (lookup.IsFailure)
            return Results.NotFound(lookup.Error.Description);

        var actor = user.Identity?.Name
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "unknown";

        db.ReceiptReprintAudits.Add(ReceiptReprintAudit.Create(
            contextResult.Value.BusinessId,
            contextResult.Value.BranchId,
            lookup.Value.SaleId,
            lookup.Value.Number,
            actor,
            "SaleNumber"));

        await db.SaveChangesAsync(ct);

        var bytes = await receiptService.GenerateSaleReceiptAsync(lookup.Value.SaleId, ct);
        return Results.File(bytes, "application/pdf", $"ticket-{lookup.Value.Number}.pdf");
    }).RequireAuthorization(policy => policy.RequireRole("Supervisor", "Administrator"));

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


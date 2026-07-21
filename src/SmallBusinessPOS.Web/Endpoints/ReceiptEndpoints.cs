using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Sales.GetSaleByNumber;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Web.Services;

namespace SmallBusinessPOS.Web.Endpoints;

public static class ReceiptEndpoints
{
    public static IEndpointRouteBuilder MapReceiptEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/receipts/sale/{saleId:guid}", GenerateSaleReceiptAsync)
            .RequireAuthorization(policy => policy.RequireRole("Cashier", "Supervisor", "Administrator"));

        endpoints.MapGet("/api/receipts/sale/{saleId:guid}/thermal", GenerateThermalSaleReceiptAsync)
            .RequireAuthorization(policy => policy.RequireRole("Cashier", "Supervisor", "Administrator"));

        endpoints.MapGet("/api/receipts/sale/by-number/{number}", GenerateSaleReceiptByNumberAsync)
            .RequireAuthorization(policy => policy.RequireRole("Supervisor", "Administrator"));

        endpoints.MapGet("/api/receipts/reprint/sale/{saleId:guid}", ReprintSaleReceiptAsync)
            .RequireAuthorization(policy => policy.RequireRole("Supervisor", "Administrator"));

        endpoints.MapGet("/api/receipts/reprint/sale/{saleId:guid}/thermal", ReprintThermalSaleReceiptAsync)
            .RequireAuthorization(policy => policy.RequireRole("Supervisor", "Administrator"));

        endpoints.MapGet("/api/receipts/reprint/by-number/{number}", ReprintSaleReceiptByNumberAsync)
            .RequireAuthorization(policy => policy.RequireRole("Supervisor", "Administrator"));

        endpoints.MapGet("/api/receipts/reprint/by-number/{number}/thermal", ReprintThermalSaleReceiptByNumberAsync)
            .RequireAuthorization(policy => policy.RequireRole("Supervisor", "Administrator"));

        return endpoints;
    }

    private static async Task<IResult> GenerateSaleReceiptAsync(
        Guid saleId,
        IReceiptService receiptService,
        CancellationToken ct)
    {
        var bytes = await receiptService.GenerateSaleReceiptAsync(saleId, ct);
        return Results.File(bytes, "application/pdf", $"ticket-{saleId:N}.pdf");
    }

    private static async Task<IResult> GenerateThermalSaleReceiptAsync(
        Guid saleId,
        IAppDbContext db,
        int? widthMm,
        CancellationToken ct)
    {
        var html = await ThermalReceiptHtmlRenderer.RenderSaleAsync(db, saleId, widthMm, ct);
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> GenerateSaleReceiptByNumberAsync(
        string number,
        GetPosContextHandler contextHandler,
        GetSaleByNumberHandler lookupHandler,
        IReceiptService receiptService,
        CancellationToken ct)
    {
        var lookup = await FindSaleByNumberAsync(number, contextHandler, lookupHandler, ct);
        if (lookup.IsFailure)
            return Results.NotFound(lookup.Error);

        var bytes = await receiptService.GenerateSaleReceiptAsync(lookup.Value!.SaleId, ct);
        return Results.File(bytes, "application/pdf", $"ticket-{lookup.Value.Number}.pdf");
    }

    private static async Task<IResult> ReprintSaleReceiptAsync(
        Guid saleId,
        ClaimsPrincipal user,
        GetPosContextHandler contextHandler,
        IAppDbContext db,
        IReceiptService receiptService,
        CancellationToken ct)
    {
        var sale = await FindSaleForContextAsync(saleId, contextHandler, db, ct);
        if (sale.IsFailure)
            return Results.NotFound(sale.Error);

        await AuditReprintAsync(db, sale.Value!.BusinessId, sale.Value.BranchId, sale.Value.SaleId, sale.Value.Number, user, "SaleId", ct);

        var bytes = await receiptService.GenerateSaleReceiptAsync(sale.Value.SaleId, ct);
        return Results.File(bytes, "application/pdf", $"ticket-{sale.Value.Number}.pdf");
    }

    private static async Task<IResult> ReprintThermalSaleReceiptAsync(
        Guid saleId,
        ClaimsPrincipal user,
        GetPosContextHandler contextHandler,
        IAppDbContext db,
        int? widthMm,
        CancellationToken ct)
    {
        var sale = await FindSaleForContextAsync(saleId, contextHandler, db, ct);
        if (sale.IsFailure)
            return Results.NotFound(sale.Error);

        await AuditReprintAsync(db, sale.Value!.BusinessId, sale.Value.BranchId, sale.Value.SaleId, sale.Value.Number, user, "SaleIdThermal", ct);

        var html = await ThermalReceiptHtmlRenderer.RenderSaleAsync(db, sale.Value.SaleId, widthMm, ct);
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> ReprintSaleReceiptByNumberAsync(
        string number,
        ClaimsPrincipal user,
        GetPosContextHandler contextHandler,
        GetSaleByNumberHandler lookupHandler,
        IAppDbContext db,
        IReceiptService receiptService,
        CancellationToken ct)
    {
        var lookup = await FindSaleByNumberAsync(number, contextHandler, lookupHandler, ct);
        if (lookup.IsFailure)
            return Results.NotFound(lookup.Error);

        await AuditReprintAsync(db, lookup.Value!.BusinessId, lookup.Value.BranchId, lookup.Value.SaleId, lookup.Value.Number, user, "SaleNumber", ct);

        var bytes = await receiptService.GenerateSaleReceiptAsync(lookup.Value.SaleId, ct);
        return Results.File(bytes, "application/pdf", $"ticket-{lookup.Value.Number}.pdf");
    }

    private static async Task<IResult> ReprintThermalSaleReceiptByNumberAsync(
        string number,
        ClaimsPrincipal user,
        GetPosContextHandler contextHandler,
        GetSaleByNumberHandler lookupHandler,
        IAppDbContext db,
        int? widthMm,
        CancellationToken ct)
    {
        var lookup = await FindSaleByNumberAsync(number, contextHandler, lookupHandler, ct);
        if (lookup.IsFailure)
            return Results.NotFound(lookup.Error);

        await AuditReprintAsync(db, lookup.Value!.BusinessId, lookup.Value.BranchId, lookup.Value.SaleId, lookup.Value.Number, user, "SaleNumberThermal", ct);

        var html = await ThermalReceiptHtmlRenderer.RenderSaleAsync(db, lookup.Value.SaleId, widthMm, ct);
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<EndpointResult<SaleLookup>> FindSaleByNumberAsync(
        string number,
        GetPosContextHandler contextHandler,
        GetSaleByNumberHandler lookupHandler,
        CancellationToken ct)
    {
        var contextResult = await contextHandler.HandleAsync(new GetPosContextQuery(), ct);
        if (contextResult.IsFailure)
            return EndpointResult<SaleLookup>.Failure(contextResult.Error.Description);

        var lookup = await lookupHandler.HandleAsync(new GetSaleByNumberQuery(
            contextResult.Value.BusinessId,
            number), ct);

        if (lookup.IsFailure)
            return EndpointResult<SaleLookup>.Failure(lookup.Error.Description);

        return EndpointResult<SaleLookup>.Success(new SaleLookup(
            contextResult.Value.BusinessId,
            contextResult.Value.BranchId,
            lookup.Value.SaleId,
            lookup.Value.Number));
    }

    private static async Task<EndpointResult<SaleLookup>> FindSaleForContextAsync(
        Guid saleId,
        GetPosContextHandler contextHandler,
        IAppDbContext db,
        CancellationToken ct)
    {
        var contextResult = await contextHandler.HandleAsync(new GetPosContextQuery(), ct);
        if (contextResult.IsFailure)
            return EndpointResult<SaleLookup>.Failure(contextResult.Error.Description);

        var sale = await db.Sales
            .Where(s => s.Id == saleId && s.BusinessId == contextResult.Value.BusinessId)
            .Select(s => new { s.Id, s.ReceiptNumber })
            .FirstOrDefaultAsync(ct);

        if (sale is null)
            return EndpointResult<SaleLookup>.Failure("Venta no encontrada.");

        return EndpointResult<SaleLookup>.Success(new SaleLookup(
            contextResult.Value.BusinessId,
            contextResult.Value.BranchId,
            sale.Id,
            sale.ReceiptNumber));
    }

    private static async Task AuditReprintAsync(
        IAppDbContext db,
        Guid businessId,
        Guid branchId,
        Guid saleId,
        string receiptNumber,
        ClaimsPrincipal user,
        string lookupMethod,
        CancellationToken ct)
    {
        db.ReceiptReprintAudits.Add(ReceiptReprintAudit.Create(
            businessId,
            branchId,
            saleId,
            receiptNumber,
            GetActor(user),
            lookupMethod));

        await db.SaveChangesAsync(ct);
    }

    private static string GetActor(ClaimsPrincipal user)
    {
        return user.Identity?.Name
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "unknown";
    }

    private sealed record SaleLookup(Guid BusinessId, Guid BranchId, Guid SaleId, string Number);

    private sealed record EndpointResult<T>(T? Value, string? Error)
    {
        public bool IsFailure => Error is not null;

        public static EndpointResult<T> Success(T value) => new(value, null);

        public static EndpointResult<T> Failure(string error) => new(default, error);
    }
}

using System.Security.Claims;
using SmallBusinessPOS.Application.Features.Receipts;
using SmallBusinessPOS.Application.Features.Receipts.GenerateSaleReceiptPdf;
using SmallBusinessPOS.Application.Features.Receipts.GenerateSaleReceiptPdfByNumber;
using SmallBusinessPOS.Application.Features.Receipts.GetSaleReceipt;
using SmallBusinessPOS.Application.Features.Receipts.ReprintSaleReceiptPdf;
using SmallBusinessPOS.Application.Features.Receipts.ReprintSaleReceiptPdfByNumber;
using SmallBusinessPOS.Application.Features.Receipts.ReprintSaleReceiptThermal;
using SmallBusinessPOS.Application.Features.Receipts.ReprintSaleReceiptThermalByNumber;
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
        GenerateSaleReceiptPdfHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GenerateSaleReceiptPdfQuery(saleId), ct);
        return result.IsFailure
            ? Results.NotFound(result.Error)
            : ReceiptFile(result.Value);
    }

    private static async Task<IResult> GenerateThermalSaleReceiptAsync(
        Guid saleId,
        GetSaleReceiptHandler handler,
        int? widthMm,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetSaleReceiptQuery(saleId), ct);
        if (result.IsFailure)
            return Results.NotFound(result.Error);

        var html = await ThermalReceiptHtmlRenderer.RenderSaleAsync(result.Value, widthMm, ct);
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> GenerateSaleReceiptByNumberAsync(
        string number,
        GenerateSaleReceiptPdfByNumberHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GenerateSaleReceiptPdfByNumberQuery(number), ct);
        return result.IsFailure
            ? Results.NotFound(result.Error)
            : ReceiptFile(result.Value);
    }

    private static async Task<IResult> ReprintSaleReceiptAsync(
        Guid saleId,
        ClaimsPrincipal user,
        ReprintSaleReceiptPdfHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ReprintSaleReceiptPdfCommand(saleId, GetActor(user)), ct);
        return result.IsFailure
            ? Results.NotFound(result.Error)
            : ReceiptFile(result.Value);
    }

    private static async Task<IResult> ReprintThermalSaleReceiptAsync(
        Guid saleId,
        ClaimsPrincipal user,
        ReprintSaleReceiptThermalHandler handler,
        int? widthMm,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ReprintSaleReceiptThermalCommand(saleId, GetActor(user)), ct);
        if (result.IsFailure)
            return Results.NotFound(result.Error);

        var html = await ThermalReceiptHtmlRenderer.RenderSaleAsync(result.Value, widthMm, ct);
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> ReprintSaleReceiptByNumberAsync(
        string number,
        ClaimsPrincipal user,
        ReprintSaleReceiptPdfByNumberHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ReprintSaleReceiptPdfByNumberCommand(number, GetActor(user)), ct);
        return result.IsFailure
            ? Results.NotFound(result.Error)
            : ReceiptFile(result.Value);
    }

    private static async Task<IResult> ReprintThermalSaleReceiptByNumberAsync(
        string number,
        ClaimsPrincipal user,
        ReprintSaleReceiptThermalByNumberHandler handler,
        int? widthMm,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new ReprintSaleReceiptThermalByNumberCommand(number, GetActor(user)), ct);
        if (result.IsFailure)
            return Results.NotFound(result.Error);

        var html = await ThermalReceiptHtmlRenderer.RenderSaleAsync(result.Value, widthMm, ct);
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static IResult ReceiptFile(ReceiptFileDto receipt) =>
        Results.File(receipt.Content, "application/pdf", receipt.FileName);

    private static string GetActor(ClaimsPrincipal user)
    {
        return user.Identity?.Name
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "unknown";
    }
}

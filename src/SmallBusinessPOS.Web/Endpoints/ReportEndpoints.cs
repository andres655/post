using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Reports.GetProfitabilityReport;
using SmallBusinessPOS.Application.Features.Sales.GetDailyReport;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Web.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/reports/daily/{date:datetime}/{format}", ExportDailyReportAsync)
            .RequireAuthorization(policy => policy.RequireRole("Supervisor", "Administrator"));

        endpoints.MapGet("/api/reports/profitability/{from:datetime}/{to:datetime}/{format}", ExportProfitabilityReportAsync)
            .RequireAuthorization(policy => policy.RequireRole("Supervisor", "Administrator"));

        return endpoints;
    }

    private static async Task<IResult> ExportDailyReportAsync(
        DateTime date,
        string format,
        GetPosContextHandler contextHandler,
        GetDailyReportHandler reportHandler,
        IReportExportService exportService,
        CancellationToken ct)
    {
        var contextResult = await contextHandler.HandleAsync(new GetPosContextQuery(), ct);
        if (contextResult.IsFailure)
            return Results.NotFound(contextResult.Error.Description);

        var reportResult = await reportHandler.HandleAsync(new GetDailyReportQuery(
            contextResult.Value.BusinessId,
            contextResult.Value.BranchId,
            DateOnly.FromDateTime(date.Date)), ct);

        if (reportResult.IsFailure)
            return Results.BadRequest(reportResult.Error.Description);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            return Results.File(exportService.ExportDailyReportCsv(reportResult.Value), "text/csv; charset=utf-8", $"reporte-diario-{reportResult.Value.Date:yyyyMMdd}.csv");

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            return Results.File(exportService.ExportDailyReportPdf(reportResult.Value), "application/pdf", $"reporte-diario-{reportResult.Value.Date:yyyyMMdd}.pdf");

        return Results.BadRequest("Formato no soportado. Use csv o pdf.");
    }

    private static async Task<IResult> ExportProfitabilityReportAsync(
        DateTime from,
        DateTime to,
        string format,
        GetPosContextHandler contextHandler,
        GetProfitabilityReportHandler reportHandler,
        IReportExportService exportService,
        CancellationToken ct)
    {
        var contextResult = await contextHandler.HandleAsync(new GetPosContextQuery(), ct);
        if (contextResult.IsFailure)
            return Results.NotFound(contextResult.Error.Description);

        var reportResult = await reportHandler.HandleAsync(new GetProfitabilityReportQuery(
            contextResult.Value.BusinessId,
            contextResult.Value.BranchId,
            DateOnly.FromDateTime(from.Date),
            DateOnly.FromDateTime(to.Date)), ct);

        if (reportResult.IsFailure)
            return Results.BadRequest(reportResult.Error.Description);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            return Results.File(exportService.ExportProfitabilityReportCsv(reportResult.Value), "text/csv; charset=utf-8", $"rentabilidad-{reportResult.Value.From:yyyyMMdd}-{reportResult.Value.To:yyyyMMdd}.csv");

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            return Results.File(exportService.ExportProfitabilityReportPdf(reportResult.Value), "application/pdf", $"rentabilidad-{reportResult.Value.From:yyyyMMdd}-{reportResult.Value.To:yyyyMMdd}.pdf");

        return Results.BadRequest("Formato no soportado. Use csv o pdf.");
    }
}

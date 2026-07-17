namespace SmallBusinessPOS.Application.Features.Reports.GetProfitabilityReport;

public sealed record GetProfitabilityReportQuery(
    Guid BusinessId,
    Guid BranchId,
    DateOnly From,
    DateOnly To);

public sealed record ProfitabilityProductDto(
    string ProductCode,
    string ProductName,
    decimal Quantity,
    decimal SalesAmount,
    decimal EstimatedCost,
    decimal GrossMargin,
    decimal GrossMarginPercent);

public sealed record ProfitabilityDailyDto(
    DateOnly Date,
    decimal SalesAmount,
    decimal EstimatedCost,
    decimal GrossMargin,
    decimal Expenses,
    decimal NetProfit);

public sealed record ProfitabilityReportDto(
    DateOnly From,
    DateOnly To,
    decimal GrossSales,
    decimal CancelledAmount,
    decimal NetSales,
    decimal EstimatedCost,
    decimal GrossMargin,
    decimal GrossMarginPercent,
    decimal Expenses,
    decimal NetProfit,
    IReadOnlyList<ProfitabilityProductDto> Products,
    IReadOnlyList<ProfitabilityDailyDto> Daily);

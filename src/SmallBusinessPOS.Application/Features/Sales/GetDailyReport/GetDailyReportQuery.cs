namespace SmallBusinessPOS.Application.Features.Sales.GetDailyReport;

public sealed record GetDailyReportQuery(Guid BusinessId, Guid BranchId, DateOnly Date);

public sealed record DailyPaymentSummaryDto(string PaymentMethod, decimal Amount);
public sealed record DailyTopProductDto(string ProductCode, string ProductName, decimal Quantity, decimal SalesAmount);
public sealed record DailySaleSummaryDto(Guid SaleId, string Number, DateTime SoldAtUtc, string Status, decimal Total);
public sealed record DailyLowStockDto(string ProductCode, string ProductName, decimal Quantity, decimal Minimum);

public sealed record DailyReportDto(
    DateOnly Date,
    decimal GrossSales,
    decimal Discounts,
    decimal CancelledAmount,
    decimal NetSales,
    decimal Expenses,
    int SalesCount,
    decimal CashSales,
    decimal ExpectedCash,
    decimal PollosPrepared,
    decimal PollosSoldEquivalent,
    decimal PollosAvailable,
    decimal Waste,
    List<DailyPaymentSummaryDto> SalesByPaymentMethod,
    List<DailyTopProductDto> TopProducts,
    List<DailySaleSummaryDto> Sales,
    List<DailyLowStockDto> LowStockItems);

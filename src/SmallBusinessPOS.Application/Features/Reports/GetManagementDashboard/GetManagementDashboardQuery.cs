namespace SmallBusinessPOS.Application.Features.Reports.GetManagementDashboard;

public sealed record GetManagementDashboardQuery(Guid BusinessId, Guid BranchId, DateOnly Today);

public sealed record DashboardKpiDto(
    decimal TodaySales,
    int TodaySalesCount,
    decimal TodayGrossMargin,
    decimal TodayExpenses,
    decimal TodayNetProfit,
    decimal ExpectedCash,
    bool HasOpenCashSession,
    decimal WeekSales,
    decimal WeekNetProfit,
    int LowStockCount,
    decimal PollosAvailable,
    decimal PollosPreparedToday,
    decimal PollosSoldToday,
    decimal WasteToday,
    decimal TodayGrossMarginPercent);

public sealed record DashboardProductDto(
    string ProductCode,
    string ProductName,
    decimal Quantity,
    decimal SalesAmount,
    decimal GrossMargin);

public sealed record DashboardActivityDto(
    DateTime OccurredAtUtc,
    string Area,
    string Action,
    string Reference,
    decimal? Amount);

public sealed record DashboardLowStockDto(
    string ProductCode,
    string ProductName,
    decimal Quantity,
    decimal MinimumQuantity);

public sealed record ManagementDashboardDto(
    DateOnly Today,
    DashboardKpiDto Kpis,
    IReadOnlyList<DashboardProductDto> TopProducts,
    IReadOnlyList<DashboardActivityDto> RecentActivity,
    IReadOnlyList<DashboardLowStockDto> LowStockItems);

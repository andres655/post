namespace SmallBusinessPOS.Application.Features.CashSessions.GetCashSessionSummary;

public sealed record GetCashSessionSummaryQuery(Guid CashRegisterId);

public sealed record CashSessionSummaryDto(
    Guid CashSessionId,
    Guid CashRegisterId,
    string CashRegisterCode,
    string CashRegisterName,
    DateTime OpenedAtUtc,
    DateTime? ClosedAtUtc,
    string Status,
    decimal OpeningAmount,
    decimal CashSales,
    decimal OtherIncome,
    decimal Expenses,
    decimal Withdrawals,
    decimal Refunds,
    decimal ClosingAdjustments,
    decimal ExpectedCash,
    decimal? CountedCash,
    decimal? Difference);

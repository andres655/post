namespace SmallBusinessPOS.Application.Features.CashSessions.DTOs;

public sealed record CashSessionDto(
    Guid Id,
    Guid CashRegisterId,
    string CashRegisterCode,
    string CashRegisterName,
    DateTime OpenedAtUtc,
    decimal OpeningBalance,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal ExpectedClosingBalance,
    decimal? DeclaredClosingBalance,
    decimal? Difference,
    string Status);

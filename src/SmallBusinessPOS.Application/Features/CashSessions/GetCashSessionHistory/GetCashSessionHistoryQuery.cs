namespace SmallBusinessPOS.Application.Features.CashSessions.GetCashSessionHistory;

public sealed record GetCashSessionHistoryQuery(
    Guid CashRegisterId,
    DateOnly? From = null,
    DateOnly? To = null);

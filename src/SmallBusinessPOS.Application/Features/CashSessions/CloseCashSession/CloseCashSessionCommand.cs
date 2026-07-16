namespace SmallBusinessPOS.Application.Features.CashSessions.CloseCashSession;

public sealed record CloseCashSessionCommand(
    Guid CashSessionId,
    decimal CountedAmount,
    string? Notes);

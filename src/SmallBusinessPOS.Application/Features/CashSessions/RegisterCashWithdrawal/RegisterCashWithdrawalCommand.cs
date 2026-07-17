namespace SmallBusinessPOS.Application.Features.CashSessions.RegisterCashWithdrawal;

public sealed record RegisterCashWithdrawalCommand(
    Guid CashRegisterId,
    decimal Amount,
    string Reason,
    string? Notes);

public sealed record RegisterCashWithdrawalResultDto(
    Guid CashMovementId,
    Guid CashSessionId,
    decimal Amount,
    decimal ExpectedCash);

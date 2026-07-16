namespace SmallBusinessPOS.Application.Features.CashSessions.OpenCashSession;

public sealed record OpenCashSessionCommand(
    Guid BusinessId,
    Guid BranchId,
    Guid CashRegisterId,
    decimal OpeningAmount);

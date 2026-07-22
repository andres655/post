namespace SmallBusinessPOS.Application.Features.POS.GetPosContext;

public sealed record GetPosContextQuery(Guid? BusinessId = null, Guid? BranchId = null, Guid? CashRegisterId = null);

public sealed record PosContextDto(
    Guid BusinessId,
    string BusinessName,
    Guid BranchId,
    string BranchName,
    Guid CashRegisterId,
    string CashRegisterCode,
    bool HasOpenCashSession,
    Guid? OpenCashSessionId);

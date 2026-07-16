namespace SmallBusinessPOS.Application.Features.POS.GetPosContext;

public sealed record GetPosContextQuery();

public sealed record PosContextDto(
    Guid BusinessId,
    string BusinessName,
    Guid BranchId,
    string BranchName,
    Guid CashRegisterId,
    string CashRegisterCode,
    bool HasOpenCashSession,
    Guid? OpenCashSessionId);

namespace SmallBusinessPOS.Application.Features.POS.GetPosOptions;

public sealed record GetPosOptionsQuery(Guid BusinessId);

public sealed record PosBranchOptionDto(Guid Id, string Name, bool IsMain);

public sealed record PosCashRegisterOptionDto(Guid Id, Guid BranchId, string Code, string Name);

public sealed record PosOptionsDto(
    List<PosBranchOptionDto> Branches,
    List<PosCashRegisterOptionDto> CashRegisters);

namespace SmallBusinessPOS.Application.Features.Reports.GetOperationalAudit;

public sealed record GetOperationalAuditQuery(
    Guid BusinessId,
    Guid BranchId,
    DateOnly From,
    DateOnly To,
    string? User = null,
    string? Area = null,
    int Take = 200);

public sealed record OperationalAuditEntryDto(
    DateTime OccurredAtUtc,
    string User,
    string Area,
    string Action,
    string EntityType,
    Guid EntityId,
    string Reference,
    decimal? Amount,
    string Details);

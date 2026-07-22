namespace SmallBusinessPOS.Application.Features.Settings.GetBusinessSettings;

public sealed record GetBusinessSettingsQuery(Guid BusinessId, Guid BranchId);

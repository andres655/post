namespace SmallBusinessPOS.Application.Features.Users.GetUsers;

public sealed record GetUsersQuery(Guid BusinessId, string? CurrentUserId);

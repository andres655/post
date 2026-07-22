namespace SmallBusinessPOS.Application.Features.Users.UpdateUserRoles;

public sealed record UpdateUserRolesCommand(
    string UserId,
    IReadOnlyCollection<string> Roles,
    bool IsCurrentUser);

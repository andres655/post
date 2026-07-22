using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Users.UpdateUserRoles;

public sealed class UpdateUserRolesHandler(IUserAdministrationService users)
{
    public Task<Result> HandleAsync(
        UpdateUserRolesCommand command,
        CancellationToken ct = default)
    {
        if (command.Roles.Count == 0)
            return Task.FromResult(Result.Failure(
                Error.Validation("Roles", "El usuario debe tener al menos un rol.")));

        if (command.IsCurrentUser && !command.Roles.Contains("Administrator"))
        {
            return Task.FromResult(Result.Failure(
                Error.BusinessRule("Users.CannotRemoveOwnAdmin", "No puedes quitarte tu propio rol de administrador.")));
        }

        return users.UpdateRolesAsync(command.UserId, command.Roles, command.IsCurrentUser, ct);
    }
}

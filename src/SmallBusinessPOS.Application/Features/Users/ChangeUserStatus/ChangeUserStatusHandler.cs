using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Users.ChangeUserStatus;

public sealed class ChangeUserStatusHandler(IUserAdministrationService users)
{
    public Task<Result<bool>> HandleAsync(
        ChangeUserStatusCommand command,
        CancellationToken ct = default)
    {
        if (command.IsCurrentUser)
        {
            return Task.FromResult(Result.Failure<bool>(
                Error.BusinessRule("Users.CannotDeactivateSelf", "No puedes desactivar tu propia sesion.")));
        }

        return users.ChangeActiveStatusAsync(
            command.UserId,
            command.CurrentIsActive,
            command.IsCurrentUser,
            ct);
    }
}

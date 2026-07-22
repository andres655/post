using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Users.ResetUserPassword;

public sealed class ResetUserPasswordHandler(IUserAdministrationService users)
{
    public Task<Result> HandleAsync(
        ResetUserPasswordCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            return Task.FromResult(Result.Failure(
                Error.Validation("NewPassword", "La nueva clave es obligatoria.")));
        }

        return users.ResetPasswordAsync(command.UserId, command.NewPassword, ct);
    }
}

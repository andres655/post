using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Users.DTOs;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Users.CreateUser;

public sealed class CreateUserHandler(
    IUserAdministrationService users,
    CreateUserValidator validator)
{
    public async Task<Result> HandleAsync(
        CreateUserCommand command,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var error = validation.Errors[0];
            return Result.Failure(Error.Validation(error.PropertyName, error.ErrorMessage));
        }

        return await users.CreateUserAsync(new CreateUserRequest(
            command.BusinessId,
            command.BranchId,
            command.FirstName,
            command.LastName,
            command.Email,
            command.Password,
            command.Role), ct);
    }
}

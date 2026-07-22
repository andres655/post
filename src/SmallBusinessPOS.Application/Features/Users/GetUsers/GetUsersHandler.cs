using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Users.DTOs;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Users.GetUsers;

public sealed class GetUsersHandler(IUserAdministrationService users)
{
    public async Task<Result<GetUsersResultDto>> HandleAsync(
        GetUsersQuery query,
        CancellationToken ct = default)
    {
        var roles = await users.GetAvailableRolesAsync(ct);
        var rows = await users.GetUsersAsync(query.BusinessId, query.CurrentUserId, ct);

        return Result.Success(new GetUsersResultDto(roles, rows));
    }
}

public sealed record GetUsersResultDto(
    List<string> AvailableRoles,
    List<UserAdministrationRowDto> Users);

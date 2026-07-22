using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Users.DTOs;

namespace SmallBusinessPOS.Application.Interfaces;

public interface IUserAdministrationService
{
    Task<List<string>> GetAvailableRolesAsync(CancellationToken ct = default);

    Task<List<UserAdministrationRowDto>> GetUsersAsync(
        Guid businessId,
        string? currentUserId,
        CancellationToken ct = default);

    Task<Result> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);

    Task<Result> UpdateRolesAsync(
        string userId,
        IReadOnlyCollection<string> roles,
        bool isCurrentUser,
        CancellationToken ct = default);

    Task<Result> ResetPasswordAsync(
        string userId,
        string newPassword,
        CancellationToken ct = default);

    Task<Result<bool>> ChangeActiveStatusAsync(
        string userId,
        bool currentIsActive,
        bool isCurrentUser,
        CancellationToken ct = default);
}

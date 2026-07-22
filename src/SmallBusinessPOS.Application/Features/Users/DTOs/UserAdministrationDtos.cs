namespace SmallBusinessPOS.Application.Features.Users.DTOs;

public sealed record UserAdministrationContext(Guid BusinessId, Guid BranchId);

public sealed record UserAdministrationRowDto(
    string Id,
    string Email,
    string DisplayName,
    bool IsActive,
    bool IsLockedOut,
    List<string> Roles,
    bool IsCurrentUser);

public sealed record CreateUserRequest(
    Guid BusinessId,
    Guid BranchId,
    string? FirstName,
    string? LastName,
    string Email,
    string Password,
    string Role);

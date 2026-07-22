namespace SmallBusinessPOS.Application.Features.Users.CreateUser;

public sealed record CreateUserCommand(
    Guid BusinessId,
    Guid BranchId,
    string? FirstName,
    string? LastName,
    string Email,
    string Password,
    string Role);

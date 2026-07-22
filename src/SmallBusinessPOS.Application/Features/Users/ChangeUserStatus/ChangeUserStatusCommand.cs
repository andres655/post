namespace SmallBusinessPOS.Application.Features.Users.ChangeUserStatus;

public sealed record ChangeUserStatusCommand(
    string UserId,
    bool CurrentIsActive,
    bool IsCurrentUser);

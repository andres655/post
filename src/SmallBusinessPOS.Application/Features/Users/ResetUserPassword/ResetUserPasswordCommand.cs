namespace SmallBusinessPOS.Application.Features.Users.ResetUserPassword;

public sealed record ResetUserPasswordCommand(string UserId, string NewPassword);

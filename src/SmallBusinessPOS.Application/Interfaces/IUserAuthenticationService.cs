using SmallBusinessPOS.Application.Common;

namespace SmallBusinessPOS.Application.Interfaces;

public interface IUserAuthenticationService
{
    Task<Result> SignInAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken ct = default);

    Task SignOutAsync(CancellationToken ct = default);

    Task<bool> IsActiveUserAsync(string userId, CancellationToken ct = default);
}

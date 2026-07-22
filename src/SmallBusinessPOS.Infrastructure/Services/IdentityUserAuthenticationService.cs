using Microsoft.AspNetCore.Identity;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Infrastructure.Data.Identity;

namespace SmallBusinessPOS.Infrastructure.Services;

public sealed class IdentityUserAuthenticationService(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : IUserAuthenticationService
{
    public async Task<Result> SignInAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Result.Failure(Error.Validation("Credentials", "Credenciales requeridas"));

        var normalizedEmail = email.Trim();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null || !user.IsActive)
            return InvalidCredentials();

        var result = await signInManager.PasswordSignInAsync(
            normalizedEmail,
            password,
            rememberMe,
            lockoutOnFailure: true);

        return result.Succeeded
            ? Result.Success()
            : InvalidCredentials();
    }

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        await signInManager.SignOutAsync();
    }

    public async Task<bool> IsActiveUserAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var user = await userManager.FindByIdAsync(userId);
        return user?.IsActive == true;
    }

    private static Result InvalidCredentials() =>
        Result.Failure(Error.Validation("Credentials", "Usuario o contrasena invalidos"));
}

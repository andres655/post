using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Users.DTOs;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Infrastructure.Data.Identity;

namespace SmallBusinessPOS.Infrastructure.Services;

public sealed class IdentityUserAdministrationService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IClock clock) : IUserAdministrationService
{
    private static readonly string[] DefaultRoles = ["Administrator", "Supervisor", "Cashier"];
    private const int UsersLimit = 200;

    public async Task<List<string>> GetAvailableRolesAsync(CancellationToken ct = default)
    {
        var roles = await roleManager.Roles
            .OrderBy(role => role.Name)
            .Select(role => role.Name!)
            .ToListAsync(ct);

        return roles.Count == 0
            ? DefaultRoles.ToList()
            : roles.OrderBy(RoleOrder).ThenBy(role => role).ToList();
    }

    public async Task<List<UserAdministrationRowDto>> GetUsersAsync(
        Guid businessId,
        string? currentUserId,
        CancellationToken ct = default)
    {
        var users = await userManager.Users
            .Where(user => user.BusinessId == businessId || user.BusinessId == null)
            .OrderBy(user => user.Email)
            .Take(UsersLimit)
            .ToListAsync(ct);

        var rows = new List<UserAdministrationRowDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            rows.Add(new UserAdministrationRowDto(
                user.Id,
                user.Email ?? user.UserName ?? "(sin email)",
                string.IsNullOrWhiteSpace(user.FullName)
                    ? user.Email ?? user.UserName ?? "(sin nombre)"
                    : user.FullName,
                user.IsActive,
                user.LockoutEnd is not null && user.LockoutEnd > clock.UtcNowOffset,
                roles.OrderBy(RoleOrder).ThenBy(role => role).ToList(),
                user.Id == currentUserId));
        }

        return rows;
    }

    public async Task<Result> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken ct = default)
    {
        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim(),
            BusinessId = request.BusinessId,
            BranchId = request.BranchId,
            IsActive = true,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return Result.Failure(FriendlyErrors(createResult));

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        return roleResult.Succeeded
            ? Result.Success()
            : Result.Failure(FriendlyErrors(roleResult));
    }

    public async Task<Result> UpdateRolesAsync(
        string userId,
        IReadOnlyCollection<string> roles,
        bool isCurrentUser,
        CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(Error.NotFound("User", userId));

        var current = await userManager.GetRolesAsync(user);
        var toRemove = current.Except(roles).ToList();
        var toAdd = roles.Except(current).ToList();

        if (toRemove.Count > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded)
                return Result.Failure(FriendlyErrors(removeResult));
        }

        if (toAdd.Count > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, toAdd);
            if (!addResult.Succeeded)
                return Result.Failure(FriendlyErrors(addResult));
        }

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(
        string userId,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(Error.NotFound("User", userId));

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(FriendlyErrors(result));
    }

    public async Task<Result<bool>> ChangeActiveStatusAsync(
        string userId,
        bool currentIsActive,
        bool isCurrentUser,
        CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<bool>(Error.NotFound("User", userId));

        user.IsActive = !currentIsActive;
        user.LockoutEnd = user.IsActive ? null : clock.UtcNowOffset.AddYears(100);

        var result = await userManager.UpdateAsync(user);
        return result.Succeeded
            ? Result.Success(user.IsActive)
            : Result.Failure<bool>(FriendlyErrors(result));
    }

    private static Error FriendlyErrors(IdentityResult result)
    {
        var messages = result.Errors.Select(error => error.Code switch
        {
            "DuplicateUserName" or "DuplicateEmail" => "Ya existe un usuario con ese email.",
            "PasswordTooShort" => "La clave no cumple la longitud minima.",
            "PasswordRequiresDigit" => "La clave debe incluir al menos un numero.",
            "PasswordRequiresUpper" => "La clave debe incluir al menos una mayuscula.",
            "PasswordRequiresLower" => "La clave debe incluir al menos una minuscula.",
            "PasswordRequiresNonAlphanumeric" => "La clave debe incluir al menos un caracter especial.",
            "InvalidEmail" or "InvalidUserName" => "El email no tiene un formato valido.",
            _ => "No se pudo completar la operacion."
        });

        return Error.BusinessRule("Identity.OperationFailed", string.Join(" ", messages.Distinct()));
    }

    private static int RoleOrder(string role) => role switch
    {
        "Administrator" => 0,
        "Supervisor" => 1,
        "Cashier" => 2,
        _ => 10
    };
}

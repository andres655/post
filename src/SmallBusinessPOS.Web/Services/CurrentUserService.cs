using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Web.Services;

public sealed class CurrentUserService(AuthenticationStateProvider authenticationStateProvider) : ICurrentUserService
{
    public async Task<string?> GetUserNameAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync();
        return user.Identity?.Name
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public async Task<string?> GetUserIdAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync();
        return user.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private async Task<ClaimsPrincipal> GetUserAsync()
    {
        var auth = await authenticationStateProvider.GetAuthenticationStateAsync();
        return auth.User;
    }
}

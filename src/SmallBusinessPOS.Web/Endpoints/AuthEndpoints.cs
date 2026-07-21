using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using SmallBusinessPOS.Infrastructure.Data.Identity;

namespace SmallBusinessPOS.Web.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", SignInAsync)
            .DisableAntiforgery()
            .AllowAnonymous();

        endpoints.MapGet("/logout", SignOutAsync)
            .AllowAnonymous();

        endpoints.MapGet("/access-denied", AccessDenied)
            .AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> SignInAsync(
        HttpRequest request,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        var form = await request.ReadFormAsync();
        var email = form["email"].ToString();
        var password = form["password"].ToString();
        var rememberMe = string.Equals(form["rememberMe"].ToString(), "on", StringComparison.OrdinalIgnoreCase);
        var returnUrl = form["returnUrl"].ToString();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return RedirectToLogin("Credenciales requeridas", returnUrl);

        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
            return RedirectToLogin("Usuario o contrasena invalidos", returnUrl);

        var result = await signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
            return RedirectToLogin("Usuario o contrasena invalidos", returnUrl);

        return Results.Redirect(IsLocalUrl(returnUrl) ? returnUrl : "/");
    }

    private static async Task<IResult> SignOutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.Redirect("/login");
    }

    private static IResult AccessDenied()
    {
        const string html = "<h1>Acceso denegado</h1><p>No tienes permisos para acceder a esta pagina.</p><p><a href='/'>Volver al inicio</a></p>";
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static IResult RedirectToLogin(string error, string returnUrl)
    {
        return Results.Redirect($"/login?error={WebUtility.UrlEncode(error)}&ReturnUrl={WebUtility.UrlEncode(returnUrl)}");
    }

    private static bool IsLocalUrl(string returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl)
            && returnUrl.StartsWith('/')
            && !returnUrl.StartsWith("//")
            && !returnUrl.StartsWith("/\\");
    }
}

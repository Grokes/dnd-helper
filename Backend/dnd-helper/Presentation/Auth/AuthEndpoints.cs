using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace dnd_helper.Presentation.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/register", async (
            RegisterRequest request,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = new ApplicationUser
            {
                UserName = request.Email.Trim(),
                Email = request.Email.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                    ? request.Email.Trim()
                    : request.DisplayName.Trim()
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return Results.ValidationProblem(result.Errors
                    .GroupBy(error => error.Code)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray()));
            }

            await userManager.AddToRoleAsync(user, ApplicationRoles.User);
            return Results.Ok(new { message = "Регистрация выполнена." });
        });

        endpoints.MapPost("/api/auth/login", async (
            LoginRequest request,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email.Trim());
            if (user is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["email"] = ["Пользователь с такой почтой не найден."]
                });
            }

            var result = await signInManager.PasswordSignInAsync(user.UserName!, request.Password, request.RememberMe, false);
            if (!result.Succeeded)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["password"] = ["Не удалось выполнить вход. Проверь почту и пароль."]
                });
            }

            return Results.Ok(new { message = "Вход выполнен." });
        });

        endpoints.MapPost("/api/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Ok(new { message = "Выход выполнен." });
        }).RequireAuthorization();

        endpoints.MapGet("/api/auth/me", async (
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var roles = await userManager.GetRolesAsync(user);
            return Results.Ok(new AuthUserDto(user.Id, user.Email ?? string.Empty, user.DisplayName, roles.ToList()));
        }).RequireAuthorization();

        endpoints.MapDelete("/api/auth/account", async (
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            await signInManager.SignOutAsync();
            var deleteResult = await userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                return Results.ValidationProblem(deleteResult.Errors
                    .GroupBy(error => error.Code)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray()));
            }

            return Results.Ok(new { message = "Аккаунт удалён." });
        }).RequireAuthorization();

        return endpoints;
    }
}

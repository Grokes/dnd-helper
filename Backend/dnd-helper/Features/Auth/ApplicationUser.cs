using Microsoft.AspNetCore.Identity;

namespace dnd_helper.Features.Auth;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}

public static class ApplicationRoles
{
    public const string User = "User";
    public const string GameMaster = "GameMaster";
}

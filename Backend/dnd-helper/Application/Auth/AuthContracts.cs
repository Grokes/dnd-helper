namespace dnd_helper.Application.Auth;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed record LoginRequest(string Email, string Password, bool RememberMe);

public sealed record AuthUserDto(string Id, string Email, string DisplayName, List<string> Roles);

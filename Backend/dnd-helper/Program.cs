using dnd_helper.Data;
using dnd_helper.Features.Auth;
using dnd_helper.Features.Characters;
using dnd_helper.Features.ReferenceData;
using dnd_helper.Features.Rooms;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var defaultDatabasePath = Path.Combine(builder.Environment.ContentRootPath, "Data", "dnd-helper.db");
    Directory.CreateDirectory(Path.GetDirectoryName(defaultDatabasePath)!);

    var connectionString = string.IsNullOrWhiteSpace(configuredConnectionString)
        ? $"Data Source={defaultDatabasePath}"
        : configuredConnectionString.Replace("Data/dnd-helper.db", defaultDatabasePath);

    options.UseSqlite(connectionString);
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await DatabaseBootstrapper.RecreateIfSchemaOutdatedAsync(dbContext);
    await dbContext.Database.EnsureCreatedAsync();
    await SeedData.InitializeAsync(dbContext, userManager, roleManager);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/register", async (
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

app.MapPost("/api/auth/login", async (
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

app.MapPost("/api/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok(new { message = "Выход выполнен." });
}).RequireAuthorization();

app.MapGet("/api/auth/me", async (
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

app.MapGet("/api/my/characters", async (
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
    var query = dbContext.Characters.AsNoTracking();
    if (!isGameMaster)
    {
        query = query.Where(character => character.OwnerUserId == user.Id);
    }

    var characters = await query.OrderBy(character => character.Name).ToListAsync();
    return Results.Ok(characters.Select(character => character.ToSummaryDto()));
}).RequireAuthorization();

app.MapGet("/api/characters/{id:guid}", async (
    Guid id,
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var character = await dbContext.Characters.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

    if (character is null)
    {
        return Results.NotFound();
    }

    var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
    var isOwner = character.OwnerUserId == user.Id;
    var isRoomViewer = false;

    if (!isGameMaster && !isOwner)
    {
        isRoomViewer = await dbContext.RoomMemberships
            .AsNoTracking()
            .Where(member => member.UserId == user.Id)
            .Join(
                dbContext.RoomMemberships.AsNoTracking().Where(member => member.CharacterId == id),
                currentMember => currentMember.RoomId,
                targetMember => targetMember.RoomId,
                (currentMember, targetMember) => targetMember.RoomId)
            .AnyAsync();
    }

    if (!isGameMaster && !isOwner && !isRoomViewer)
    {
        return Results.Forbid();
    }

    return Results.Ok(character.ToDto(canEdit: isGameMaster || isOwner));
}).RequireAuthorization();

app.MapGet("/api/reference/character-options", () => Results.Ok(CharacterOptionsCatalog.All));

app.MapPost("/api/characters", async (
    CreateCharacterRequest request,
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var race = CharacterOptionsCatalog.Races.FirstOrDefault(item => item.Id == request.RaceId);
    var characterClass = CharacterOptionsCatalog.Classes.FirstOrDefault(item => item.Id == request.ClassId);
    var background = CharacterOptionsCatalog.Backgrounds.FirstOrDefault(item => item.Id == request.BackgroundId);

    var validationError = CharacterBuilder.Validate(request, race, characterClass, background);
    if (validationError is not null)
    {
        return Results.ValidationProblem(validationError);
    }

    var computedCharacter = CharacterBuilder.Compute(
        request.Name,
        race!,
        characterClass!,
        background!,
        request.Level,
        request.Alignment,
        request.Notes,
        request.BaseAbilities,
        request.BonusAbilitySelections,
        request.RaceSkillSelections,
        request.ClassSkillSelections,
        request.Spells,
        request.Inventory);

    var character = CharacterEntity.FromComputed(computedCharacter, user.Id);

    dbContext.Characters.Add(character);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/characters/{character.Id}", character.ToDto());
}).RequireAuthorization();

app.MapPut("/api/characters/{id:guid}", async (
    Guid id,
    UpdateCharacterRequest request,
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var race = CharacterOptionsCatalog.Races.FirstOrDefault(item => item.Id == request.RaceId);
    var characterClass = CharacterOptionsCatalog.Classes.FirstOrDefault(item => item.Id == request.ClassId);
    var background = CharacterOptionsCatalog.Backgrounds.FirstOrDefault(item => item.Id == request.BackgroundId);

    var validationError = CharacterBuilder.Validate(
        new CreateCharacterRequest(
            request.Name,
            request.RaceId,
            request.ClassId,
            request.BackgroundId,
            request.Level,
            request.Alignment,
            request.Notes,
            request.BaseAbilities,
            request.BonusAbilitySelections,
            request.RaceSkillSelections,
            request.ClassSkillSelections,
            request.Spells,
            request.Inventory),
        race,
        characterClass,
        background);
    if (validationError is not null)
    {
        return Results.ValidationProblem(validationError);
    }

    var character = await dbContext.Characters.FirstOrDefaultAsync(item => item.Id == id);
    if (character is null)
    {
        return Results.NotFound();
    }

    var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
    if (!isGameMaster && character.OwnerUserId != user.Id)
    {
        return Results.Forbid();
    }

    var computedCharacter = CharacterBuilder.Compute(
        request.Name,
        race!,
        characterClass!,
        background!,
        request.Level,
        request.Alignment,
        request.Notes,
        request.BaseAbilities,
        request.BonusAbilitySelections,
        request.RaceSkillSelections,
        request.ClassSkillSelections,
        request.Spells,
        request.Inventory);

    character.UpdateFromComputed(computedCharacter);
    await dbContext.SaveChangesAsync();

    return Results.Ok(character.ToDto());
}).RequireAuthorization();

app.MapGet("/api/rooms", async (
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var rooms = await dbContext.Rooms
        .AsNoTracking()
        .Include(room => room.OwnerUser)
        .Include(room => room.Members)
            .ThenInclude(member => member.User)
        .Include(room => room.Members)
            .ThenInclude(member => member.Character)
        .Where(room => room.Members.Any(member => member.UserId == user.Id))
        .OrderBy(room => room.Name)
        .ToListAsync();

    return Results.Ok(rooms.Select(room => room.ToSummaryDto(user.Id)));
}).RequireAuthorization();

app.MapPost("/api/rooms", async (
    CreateRoomRequest request,
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["name"] = ["У комнаты должно быть название."]
        });
    }

    var room = new RoomEntity
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        JoinCode = GenerateJoinCode(),
        InviteToken = GenerateInviteToken(),
        OwnerUserId = user.Id,
        CreatedAtUtc = DateTime.UtcNow,
        Members =
        [
            new RoomMembershipEntity
            {
                UserId = user.Id,
                Role = RoomMemberRoles.GameMaster,
                JoinedAtUtc = DateTime.UtcNow,
                LastSeenAtUtc = DateTime.UtcNow
            }
        ]
    };

    while (await dbContext.Rooms.AnyAsync(existingRoom => existingRoom.JoinCode == room.JoinCode))
    {
        room.JoinCode = GenerateJoinCode();
    }

    while (await dbContext.Rooms.AnyAsync(existingRoom => existingRoom.InviteToken == room.InviteToken))
    {
        room.InviteToken = GenerateInviteToken();
    }

    dbContext.Rooms.Add(room);
    await dbContext.SaveChangesAsync();

    var hydratedRoom = await dbContext.Rooms
        .AsNoTracking()
        .Include(existingRoom => existingRoom.OwnerUser)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.User)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.Character)
        .FirstAsync(existingRoom => existingRoom.Id == room.Id);

    return Results.Created($"/api/rooms/{room.Id}", hydratedRoom.ToDto(user.Id));
}).RequireAuthorization();

app.MapPost("/api/rooms/join", async (
    JoinRoomRequest request,
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(request.JoinCode))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["joinCode"] = ["Укажите код комнаты."]
        });
    }

    var joinCode = request.JoinCode.Trim().ToUpperInvariant();
    var room = await dbContext.Rooms
        .Include(existingRoom => existingRoom.OwnerUser)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.User)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.Character)
        .FirstOrDefaultAsync(existingRoom => existingRoom.JoinCode == joinCode);

    if (room is null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["joinCode"] = ["Комната с таким кодом не найдена."]
        });
    }

    if (!room.Members.Any(member => member.UserId == user.Id))
    {
        room.Members.Add(new RoomMembershipEntity
        {
            RoomId = room.Id,
            UserId = user.Id,
            Role = RoomMemberRoles.Player,
            JoinedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }
    else
    {
        var existingMembership = room.Members.First(member => member.UserId == user.Id);
        existingMembership.LastSeenAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    var refreshedRoom = await dbContext.Rooms
        .AsNoTracking()
        .Include(existingRoom => existingRoom.OwnerUser)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.User)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.Character)
        .FirstAsync(existingRoom => existingRoom.Id == room.Id);

    return Results.Ok(refreshedRoom.ToDto(user.Id));
}).RequireAuthorization();

app.MapPost("/api/rooms/join/invite", async (
    JoinRoomByInviteRequest request,
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(request.InviteToken))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["inviteToken"] = ["Укажите ссылку-приглашение."]
        });
    }

    var inviteToken = request.InviteToken.Trim();
    var room = await dbContext.Rooms
        .Include(existingRoom => existingRoom.OwnerUser)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.User)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.Character)
        .FirstOrDefaultAsync(existingRoom => existingRoom.InviteToken == inviteToken);

    if (room is null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["inviteToken"] = ["Приглашение недействительно или уже устарело."]
        });
    }

    if (!room.Members.Any(member => member.UserId == user.Id))
    {
        room.Members.Add(new RoomMembershipEntity
        {
            RoomId = room.Id,
            UserId = user.Id,
            Role = RoomMemberRoles.Player,
            JoinedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow
        });
    }
    else
    {
        var existingMembership = room.Members.First(member => member.UserId == user.Id);
        existingMembership.LastSeenAtUtc = DateTime.UtcNow;
    }

    await dbContext.SaveChangesAsync();

    var refreshedRoom = await dbContext.Rooms
        .AsNoTracking()
        .Include(existingRoom => existingRoom.OwnerUser)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.User)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.Character)
        .FirstAsync(existingRoom => existingRoom.Id == room.Id);

    return Results.Ok(refreshedRoom.ToDto(user.Id));
}).RequireAuthorization();

app.MapGet("/api/rooms/{id:guid}", async (
    Guid id,
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var room = await dbContext.Rooms
        .AsNoTracking()
        .Include(existingRoom => existingRoom.OwnerUser)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.User)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.Character)
        .FirstOrDefaultAsync(existingRoom => existingRoom.Id == id);

    if (room is null)
    {
        return Results.NotFound();
    }

    if (!room.Members.Any(member => member.UserId == user.Id))
    {
        return Results.Forbid();
    }

    return Results.Ok(room.ToDto(user.Id));
}).RequireAuthorization();

app.MapPut("/api/rooms/{id:guid}/character", async (
    Guid id,
    SelectRoomCharacterRequest request,
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var membership = await dbContext.RoomMemberships
        .Include(member => member.Room)
        .FirstOrDefaultAsync(member => member.RoomId == id && member.UserId == user.Id);

    if (membership is null)
    {
        return Results.Forbid();
    }

    if (request.CharacterId is not null)
    {
        var character = await dbContext.Characters.FirstOrDefaultAsync(existingCharacter =>
            existingCharacter.Id == request.CharacterId.Value && existingCharacter.OwnerUserId == user.Id);

        if (character is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["characterId"] = ["Можно выбрать только собственного персонажа."]
            });
        }

        membership.CharacterId = character.Id;
    }
    else
    {
        membership.CharacterId = null;
    }

    membership.LastSeenAtUtc = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();

    var room = await dbContext.Rooms
        .AsNoTracking()
        .Include(existingRoom => existingRoom.OwnerUser)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.User)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.Character)
        .FirstAsync(existingRoom => existingRoom.Id == id);

    return Results.Ok(room.ToDto(user.Id));
}).RequireAuthorization();

app.MapPost("/api/rooms/{id:guid}/presence", async (
    Guid id,
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var membership = await dbContext.RoomMemberships
        .FirstOrDefaultAsync(member => member.RoomId == id && member.UserId == user.Id);

    if (membership is null)
    {
        return Results.Forbid();
    }

    membership.LastSeenAtUtc = DateTime.UtcNow;
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapPut("/api/rooms/{id:guid}/members/{memberUserId}/role", async (
    Guid id,
    string memberUserId,
    UpdateRoomMemberRoleRequest request,
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var currentMembership = await dbContext.RoomMemberships
        .Include(member => member.Room)
        .FirstOrDefaultAsync(member => member.RoomId == id && member.UserId == user.Id);

    if (currentMembership?.Room is null)
    {
        return Results.Forbid();
    }

    if (currentMembership.Role != RoomMemberRoles.GameMaster && currentMembership.Room.OwnerUserId != user.Id)
    {
        return Results.Forbid();
    }

    var normalizedRole = NormalizeRoomRole(request.Role);
    if (normalizedRole is null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["role"] = ["Роль комнаты должна быть GameMaster или Player."]
        });
    }

    var targetMembership = await dbContext.RoomMemberships
        .FirstOrDefaultAsync(member => member.RoomId == id && member.UserId == memberUserId);

    if (targetMembership is null)
    {
        return Results.NotFound();
    }

    if (memberUserId == currentMembership.Room.OwnerUserId && normalizedRole != RoomMemberRoles.GameMaster)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["role"] = ["Владелец комнаты всегда остаётся ведущим."]
        });
    }

    targetMembership.Role = normalizedRole;
    await dbContext.SaveChangesAsync();

    var room = await dbContext.Rooms
        .AsNoTracking()
        .Include(existingRoom => existingRoom.OwnerUser)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.User)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.Character)
        .FirstAsync(existingRoom => existingRoom.Id == id);

    return Results.Ok(room.ToDto(user.Id));
}).RequireAuthorization();

app.MapPut("/api/rooms/{id:guid}/session", async (
    Guid id,
    UpdateRoomSessionRequest request,
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) =>
{
    var user = await userManager.GetUserAsync(principal);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var currentMembership = await dbContext.RoomMemberships
        .Include(member => member.Room)
        .FirstOrDefaultAsync(member => member.RoomId == id && member.UserId == user.Id);

    if (currentMembership?.Room is null)
    {
        return Results.Forbid();
    }

    if (currentMembership.Role != RoomMemberRoles.GameMaster && currentMembership.Room.OwnerUserId != user.Id)
    {
        return Results.Forbid();
    }

    if (request.ActiveMemberUserId is null)
    {
        currentMembership.Room.ActiveMemberUserId = null;
        currentMembership.Room.SessionUpdatedAtUtc = DateTime.UtcNow;
    }
    else
    {
        var targetMembership = await dbContext.RoomMemberships
            .FirstOrDefaultAsync(member => member.RoomId == id && member.UserId == request.ActiveMemberUserId);

        if (targetMembership is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["activeMemberUserId"] = ["Выбранный участник не состоит в комнате."]
            });
        }

        currentMembership.Room.ActiveMemberUserId = targetMembership.UserId;
        currentMembership.Room.SessionUpdatedAtUtc = DateTime.UtcNow;
    }

    await dbContext.SaveChangesAsync();

    var room = await dbContext.Rooms
        .AsNoTracking()
        .Include(existingRoom => existingRoom.OwnerUser)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.User)
        .Include(existingRoom => existingRoom.Members)
            .ThenInclude(member => member.Character)
        .FirstAsync(existingRoom => existingRoom.Id == id);

    return Results.Ok(room.ToDto(user.Id));
}).RequireAuthorization();

app.Run();

static string GenerateJoinCode()
{
    const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    var random = Random.Shared;
    return new string(Enumerable.Range(0, 6).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray());
}

static string GenerateInviteToken()
{
    return Guid.NewGuid().ToString("N");
}

static string? NormalizeRoomRole(string role)
{
    var normalized = role.Trim().ToLowerInvariant();
    return normalized switch
    {
        "gamemaster" => RoomMemberRoles.GameMaster,
        "player" => RoomMemberRoles.Player,
        _ => null
    };
}

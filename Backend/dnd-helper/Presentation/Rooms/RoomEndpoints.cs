using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace dnd_helper.Presentation.Rooms;

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/rooms", async (
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            RoomAccessService roomAccessService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var rooms = await dbContext.Rooms
                .AsNoTracking()
                .Include(room => room.OwnerUser)
                .Include(room => room.Members).ThenInclude(member => member.User)
                .Include(room => room.Members).ThenInclude(member => member.Characters).ThenInclude(link => link.Character)
                .Where(room => room.Members.Any(member => member.UserId == user.Id))
                .OrderBy(room => room.Name)
                .ToListAsync();

            return Results.Ok(rooms.Select(room => room.ToSummaryDto(user.Id)));
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms", async (
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
                        InventoryJson = "[]",
                        JoinedAtUtc = DateTime.UtcNow,
                        LastSeenAtUtc = DateTime.UtcNow
                    }
                ]
            };

            dbContext.Rooms.Add(room);
            await dbContext.SaveChangesAsync();

            var hydratedRoom = await dbContext.Rooms
                .AsNoTracking()
                .Include(existingRoom => existingRoom.OwnerUser)
                .Include(existingRoom => existingRoom.Members).ThenInclude(member => member.User)
                .Include(existingRoom => existingRoom.Members).ThenInclude(member => member.Characters).ThenInclude(link => link.Character)
                .FirstAsync(existingRoom => existingRoom.Id == room.Id);

            return Results.Created($"/api/rooms/{room.Id}", hydratedRoom.ToDto(user.Id));
        }).RequireAuthorization();

        endpoints.MapGet("/api/rooms/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            RoomAccessService roomAccessService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var room = await dbContext.Rooms
                .AsNoTracking()
                .Include(existingRoom => existingRoom.OwnerUser)
                .Include(existingRoom => existingRoom.Members).ThenInclude(member => member.User)
                .Include(existingRoom => existingRoom.Members).ThenInclude(member => member.Characters).ThenInclude(link => link.Character)
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

        endpoints.MapPost("/api/rooms/join", async (
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
                .Include(existingRoom => existingRoom.Members).ThenInclude(member => member.User)
                .Include(existingRoom => existingRoom.Members).ThenInclude(member => member.Characters).ThenInclude(link => link.Character)
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
                    InventoryJson = "[]",
                    JoinedAtUtc = DateTime.UtcNow,
                    LastSeenAtUtc = DateTime.UtcNow
                });
            }

            await dbContext.SaveChangesAsync();
            return Results.Ok(room.ToDto(user.Id));
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/join/invite", async (
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

            var room = await dbContext.Rooms
                .Include(existingRoom => existingRoom.OwnerUser)
                .Include(existingRoom => existingRoom.Members).ThenInclude(member => member.User)
                .Include(existingRoom => existingRoom.Members).ThenInclude(member => member.Characters).ThenInclude(link => link.Character)
                .FirstOrDefaultAsync(existingRoom => existingRoom.InviteToken == request.InviteToken.Trim());

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
                    InventoryJson = "[]",
                    JoinedAtUtc = DateTime.UtcNow,
                    LastSeenAtUtc = DateTime.UtcNow
                });
            }

            await dbContext.SaveChangesAsync();
            return Results.Ok(room.ToDto(user.Id));
        }).RequireAuthorization();

        endpoints.MapPut("/api/rooms/{id:guid}/character", async (
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
                .Include(member => member.Characters)
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

                if (!membership.Characters.Any(link => link.CharacterId == character.Id))
                {
                    membership.Characters.Add(new RoomMembershipCharacterEntity
                    {
                        RoomId = id,
                        UserId = user.Id,
                        CharacterId = character.Id
                    });
                }
            }
            else
            {
                membership.Characters.Clear();
            }

            membership.LastSeenAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            var room = await dbContext.Rooms
                .AsNoTracking()
                .Include(existingRoom => existingRoom.OwnerUser)
                .Include(existingRoom => existingRoom.Members).ThenInclude(member => member.User)
                .Include(existingRoom => existingRoom.Members).ThenInclude(member => member.Characters).ThenInclude(link => link.Character)
                .FirstAsync(existingRoom => existingRoom.Id == id);
            return Results.Ok(room.ToDto(user.Id));
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/presence", async (
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

            var membership = await dbContext.RoomMemberships.FirstOrDefaultAsync(member => member.RoomId == id && member.UserId == user.Id);
            if (membership is null)
            {
                return Results.Forbid();
            }

            membership.LastSeenAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        endpoints.MapPut("/api/rooms/{id:guid}/members/{memberUserId}/role", async (
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

            var targetMembership = await dbContext.RoomMemberships.FirstOrDefaultAsync(member => member.RoomId == id && member.UserId == memberUserId);
            if (targetMembership is null)
            {
                return Results.NotFound();
            }

            targetMembership.Role = normalizedRole;
            await dbContext.SaveChangesAsync();
            var room = await dbContext.Rooms
                .AsNoTracking()
                .Include(existingRoom => existingRoom.OwnerUser)
                .Include(existingRoom => existingRoom.Members).ThenInclude(member => member.User)
                .Include(existingRoom => existingRoom.Members).ThenInclude(member => member.Characters).ThenInclude(link => link.Character)
                .FirstAsync(existingRoom => existingRoom.Id == id);
            return Results.Ok(room.ToDto(user.Id));
        }).RequireAuthorization();

        endpoints.MapGet("/api/rooms/{id:guid}/monsters", async (
            Guid id,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            RoomAccessService roomAccessService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var membership = await roomAccessService.GetMembershipAsync(id, user.Id, cancellationToken);
            if (membership is null)
            {
                return Results.Forbid();
            }

            var monsters = await dbContext.EncounterCombatants
                .AsNoTracking()
                .Where(combatant => combatant.Encounter!.RoomId == id && !combatant.IsPlayerCharacter && combatant.MonsterSlug != null)
                .Include(combatant => combatant.Encounter)
                .OrderBy(combatant => combatant.Name)
                .ToListAsync();

            return Results.Ok(monsters.Select(RoomMonsterService.MapMonsterDto).ToList());
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/monsters", async (
            Guid id,
            AddRoomMonsterRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            RoomAccessService roomAccessService,
            RoomMonsterService monsterService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var membership = await roomAccessService.GetManagerMembershipAsync(id, user.Id, cancellationToken);
            if (membership is null)
            {
                return Results.Forbid();
            }

            var result = await monsterService.AddMonsterAsync(id, request, cancellationToken);
            if (!result.IsSuccess)
            {
                return Results.ValidationProblem(result.Errors!);
            }

            return Results.Ok(result.Result);
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/monsters/{monsterId:guid}/damage", async (
            Guid id,
            Guid monsterId,
            ApplyMonsterDamageRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            RoomAccessService roomAccessService,
            RoomMonsterService monsterService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var membership = await roomAccessService.GetManagerMembershipAsync(id, user.Id, cancellationToken);
            if (membership is null)
            {
                return Results.Forbid();
            }

            var result = await monsterService.ApplyDamageAsync(id, monsterId, request, cancellationToken);
            if (result.IsNotFound)
            {
                return Results.NotFound();
            }

            if (!result.IsSuccess)
            {
                return Results.ValidationProblem(result.Errors!);
            }

            return Results.Ok(result.Result);
        }).RequireAuthorization();

        endpoints.MapDelete("/api/rooms/{id:guid}/monsters/{monsterId:guid}", async (
            Guid id,
            Guid monsterId,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            RoomAccessService roomAccessService,
            RoomMonsterService monsterService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var membership = await roomAccessService.GetManagerMembershipAsync(id, user.Id, cancellationToken);
            if (membership is null)
            {
                return Results.Forbid();
            }

            var deleted = await monsterService.DeleteMonsterAsync(id, monsterId, cancellationToken);
            if (!deleted)
            {
                return Results.NotFound();
            }

            return Results.NoContent();
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/monsters/{monsterId:guid}/attack", async (
            Guid id,
            Guid monsterId,
            MonsterAttackRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            RoomAccessService roomAccessService,
            RoomMonsterService monsterService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var membership = await roomAccessService.GetManagerMembershipAsync(id, user.Id, cancellationToken);
            if (membership is null)
            {
                return Results.Forbid();
            }

            var result = await monsterService.AttackAsync(id, monsterId, request, cancellationToken);
            if (result.IsNotFound)
            {
                return Results.NotFound();
            }

            if (!result.IsSuccess)
            {
                return Results.ValidationProblem(result.Errors!);
            }

            return Results.Ok(result.Result);
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/monsters/{monsterId:guid}/roll-damage", async (
            Guid id,
            Guid monsterId,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            RoomAccessService roomAccessService,
            RoomMonsterService monsterService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var membership = await roomAccessService.GetManagerMembershipAsync(id, user.Id, cancellationToken);
            if (membership is null)
            {
                return Results.Forbid();
            }

            var result = await monsterService.RollDamageAsync(id, monsterId, cancellationToken);
            if (result.IsNotFound)
            {
                return Results.NotFound();
            }

            if (!result.IsSuccess)
            {
                return Results.ValidationProblem(result.Errors!);
            }

            return Results.Ok(result.Result);
        }).RequireAuthorization();

        return endpoints;
    }

    private static string GenerateJoinCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = Random.Shared;
        return new string(Enumerable.Range(0, 6).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray());
    }

    private static string GenerateInviteToken() => Guid.NewGuid().ToString("N");

    private static string? NormalizeRoomRole(string role)
    {
        var normalized = role.Trim().ToLowerInvariant();
        return normalized switch
        {
            "gamemaster" => RoomMemberRoles.GameMaster,
            "player" => RoomMemberRoles.Player,
            _ => null
        };
    }

}

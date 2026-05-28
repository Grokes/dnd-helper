using dnd_helper.Features.Auth;
using dnd_helper.Features.Rules;
using dnd_helper.Infrastructure.Persistence.Postgres;
using dnd_helper.Infrastructure.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace dnd_helper.Features.Rooms;

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/rooms", async (
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

            var monsters = await dbContext.EncounterCombatants
                .AsNoTracking()
                .Where(combatant => combatant.Encounter!.RoomId == id && !combatant.IsPlayerCharacter && combatant.MonsterSlug != null)
                .Include(combatant => combatant.Encounter)
                .OrderBy(combatant => combatant.Name)
                .ToListAsync();

            return Results.Ok(monsters.Select(MapMonsterDto).ToList());
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/monsters", async (
            Guid id,
            AddRoomMonsterRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            IRulesCatalogRepository repository,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var membership = await dbContext.RoomMemberships
                .Include(member => member.Room)
                .FirstOrDefaultAsync(member => member.RoomId == id && member.UserId == user.Id, cancellationToken);

            if (membership?.Room is null)
            {
                return Results.Forbid();
            }

            if (membership.Role != RoomMemberRoles.GameMaster && membership.Room.OwnerUserId != user.Id)
            {
                return Results.Forbid();
            }

            if (string.IsNullOrWhiteSpace(request.MonsterSlug))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["monsterSlug"] = ["Выбери чудовище из справочника."]
                });
            }

            var monstersCatalog = await repository.GetMonstersAsync(RulesDatabaseSeeder.RulesetId, cancellationToken);
            var monster = monstersCatalog.FirstOrDefault(item =>
                item.Slug.Equals(request.MonsterSlug.Trim(), StringComparison.OrdinalIgnoreCase));

            if (monster is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["monsterSlug"] = ["Такого чудовища нет в справочнике PHB."]
                });
            }

            var encounter = await dbContext.Encounters
                .Include(existingEncounter => existingEncounter.Combatants)
                .FirstOrDefaultAsync(existingEncounter => existingEncounter.RoomId == id, cancellationToken);

            if (encounter is null)
            {
                encounter = new Features.Characters.EncounterEntity
                {
                    Id = Guid.NewGuid(),
                    RoomId = id,
                    Name = "Основная сцена",
                    CreatedAtUtc = DateTime.UtcNow
                };
                dbContext.Encounters.Add(encounter);
            }

            var combatant = new Features.Characters.EncounterCombatantEntity
            {
                Id = Guid.NewGuid(),
                EncounterId = encounter.Id,
                MonsterSlug = monster.Slug,
                Name = monster.Name,
                ChallengeRating = monster.ChallengeRating,
                Initiative = 0,
                ArmorClass = monster.ArmorClass,
                MaxHitPoints = monster.HitPoints,
                CurrentHitPoints = monster.HitPoints,
                AttackName = string.IsNullOrWhiteSpace(monster.AttackName) ? "Атака" : monster.AttackName,
                AttackBonus = monster.AttackBonus,
                DamageDice = string.IsNullOrWhiteSpace(monster.DamageDice) ? "1d4" : monster.DamageDice,
                DamageBonus = monster.DamageBonus,
                DamageType = string.IsNullOrWhiteSpace(monster.DamageType) ? "bludgeoning" : monster.DamageType,
                IsPlayerCharacter = false
            };

            dbContext.EncounterCombatants.Add(combatant);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(MapMonsterDto(combatant));
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/monsters/{monsterId:guid}/damage", async (
            Guid id,
            Guid monsterId,
            ApplyMonsterDamageRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var membership = await dbContext.RoomMemberships
                .Include(member => member.Room)
                .FirstOrDefaultAsync(member => member.RoomId == id && member.UserId == user.Id, cancellationToken);

            if (membership?.Room is null)
            {
                return Results.Forbid();
            }

            if (membership.Role != RoomMemberRoles.GameMaster && membership.Room.OwnerUserId != user.Id)
            {
                return Results.Forbid();
            }

            if (request.Damage <= 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["damage"] = ["Урон должен быть больше 0."]
                });
            }

            var combatant = await dbContext.EncounterCombatants
                .Include(existingCombatant => existingCombatant.Encounter)
                .FirstOrDefaultAsync(existingCombatant =>
                    existingCombatant.Id == monsterId &&
                    existingCombatant.Encounter!.RoomId == id &&
                    !existingCombatant.IsPlayerCharacter,
                    cancellationToken);

            if (combatant is null)
            {
                return Results.NotFound();
            }

            combatant.CurrentHitPoints = Math.Max(0, combatant.CurrentHitPoints - request.Damage);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(MapMonsterDto(combatant));
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/monsters/{monsterId:guid}/roll-damage", async (
            Guid id,
            Guid monsterId,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var membership = await dbContext.RoomMemberships
                .Include(member => member.Room)
                .FirstOrDefaultAsync(member => member.RoomId == id && member.UserId == user.Id, cancellationToken);

            if (membership?.Room is null)
            {
                return Results.Forbid();
            }

            if (membership.Role != RoomMemberRoles.GameMaster && membership.Room.OwnerUserId != user.Id)
            {
                return Results.Forbid();
            }

            var combatant = await dbContext.EncounterCombatants
                .Include(existingCombatant => existingCombatant.Encounter)
                .FirstOrDefaultAsync(existingCombatant =>
                    existingCombatant.Id == monsterId &&
                    existingCombatant.Encounter!.RoomId == id &&
                    !existingCombatant.IsPlayerCharacter,
                    cancellationToken);

            if (combatant is null)
            {
                return Results.NotFound();
            }

            if (!TryRollDamage(combatant.DamageDice ?? "1d4", out var expression, out var diceResult))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["damageDice"] = ["Невозможно бросить урон: некорректный формат кости урона."]
                });
            }

            var total = Math.Max(0, diceResult + combatant.DamageBonus);
            return Results.Ok(new MonsterDamageRollDto(
                combatant.Id,
                combatant.Name,
                combatant.AttackName ?? "Атака",
                expression,
                diceResult,
                combatant.DamageBonus,
                total,
                DateTime.UtcNow));
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

    private static RoomMonsterDto MapMonsterDto(Features.Characters.EncounterCombatantEntity combatant)
    {
        return new RoomMonsterDto(
            combatant.Id,
            combatant.MonsterSlug ?? string.Empty,
            combatant.Name,
            combatant.ChallengeRating,
            combatant.ArmorClass,
            combatant.MaxHitPoints,
            combatant.CurrentHitPoints,
            combatant.AttackName ?? "Атака",
            combatant.AttackBonus,
            combatant.DamageDice ?? "1d4",
            combatant.DamageBonus,
            combatant.DamageType ?? "bludgeoning");
    }

    private static bool TryRollDamage(string sourceDice, out string expression, out int result)
    {
        expression = sourceDice;
        result = 0;

        var normalized = sourceDice.Trim().ToLowerInvariant();
        var match = System.Text.RegularExpressions.Regex.Match(normalized, @"^(?<count>\d+)d(?<sides>\d+)$");
        if (!match.Success)
        {
            return false;
        }

        var count = int.Parse(match.Groups["count"].Value);
        var sides = int.Parse(match.Groups["sides"].Value);
        if (count <= 0 || sides <= 0 || count > 30 || sides > 1000)
        {
            return false;
        }

        var rolls = new List<int>(count);
        for (var index = 0; index < count; index++)
        {
            rolls.Add(Random.Shared.Next(1, sides + 1));
        }

        result = rolls.Sum();
        expression = count > 1 ? $"{count}d{sides} ({string.Join(" + ", rolls)})" : $"{count}d{sides}";
        return true;
    }
}

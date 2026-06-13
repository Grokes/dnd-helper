using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace dnd_helper.Presentation.Characters;

public static class CharacterEndpoints
{
    public static IEndpointRouteBuilder MapCharacterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/my/characters", async (
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

        endpoints.MapGet("/api/characters/{id:guid}", async (
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
                        dbContext.RoomMembershipCharacters.AsNoTracking().Where(member => member.CharacterId == id),
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

        endpoints.MapGet("/api/characters/{id:guid}/calculation-trace", async (
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

            var character = await dbContext.Characters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (character is null)
            {
                return Results.NotFound();
            }

            var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
            var canView = isGameMaster || character.OwnerUserId == user.Id ||
                          await dbContext.RoomMemberships
                              .AsNoTracking()
                              .Where(member => member.UserId == user.Id)
                              .Join(
                                  dbContext.RoomMembershipCharacters.AsNoTracking().Where(member => member.CharacterId == id),
                                  currentMember => currentMember.RoomId,
                                  targetMember => targetMember.RoomId,
                                  (currentMember, targetMember) => targetMember.RoomId)
                              .AnyAsync();

            if (!canView)
            {
                return Results.Forbid();
            }

            return Results.Ok(character.ToDto(canEdit: isGameMaster || character.OwnerUserId == user.Id).CalculationTrace);
        }).RequireAuthorization();

        endpoints.MapPost("/api/characters", async (
            CreateCharacterRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            CharacterCreationService creationService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var createResult = await creationService.BuildCharacterAsync(request, user.Id, cancellationToken);
            if (!createResult.IsSuccess)
            {
                return Results.ValidationProblem(createResult.Errors!);
            }

            dbContext.Characters.Add(createResult.Character!);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/characters/{createResult.Character!.Id}", createResult.Character.ToDto());
        }).RequireAuthorization();

        endpoints.MapPut("/api/characters/{id:guid}", async (
            Guid id,
            UpdateCharacterRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            CharacterCreationService creationService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var character = await dbContext.Characters.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (character is null)
            {
                return Results.NotFound();
            }

            var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
            var isOwner = character.OwnerUserId == user.Id;
            if (!isGameMaster && !isOwner)
            {
                return Results.Forbid();
            }

            var normalizedRequest = new CreateCharacterRequest(
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
                request.Inventory);

            var updateResult = await creationService.BuildCharacterAsync(normalizedRequest, character.OwnerUserId, cancellationToken);
            if (!updateResult.IsSuccess)
            {
                return Results.ValidationProblem(updateResult.Errors!);
            }

            var rebuilt = updateResult.Character!;
            var previousCurrentHitPoints = character.MaxHitPoints > 0
                ? character.CurrentHitPoints
                : character.HitPoints;
            character.RaceId = rebuilt.RaceId;
            character.ClassId = rebuilt.ClassId;
            character.BackgroundId = rebuilt.BackgroundId;
            character.Name = rebuilt.Name;
            character.Race = rebuilt.Race;
            character.ClassName = rebuilt.ClassName;
            character.Subclass = rebuilt.Subclass;
            character.Level = rebuilt.Level;
            character.Background = rebuilt.Background;
            character.Alignment = rebuilt.Alignment;
            character.ArmorClass = rebuilt.ArmorClass;
            character.WeaponDamage = rebuilt.WeaponDamage;
            character.HitPoints = rebuilt.HitPoints;
            character.MaxHitPoints = rebuilt.MaxHitPoints <= 0 ? rebuilt.HitPoints : rebuilt.MaxHitPoints;
            character.CurrentHitPoints = Math.Clamp(previousCurrentHitPoints, 0, character.MaxHitPoints);
            character.Speed = rebuilt.Speed;
            character.ProficiencyBonus = rebuilt.ProficiencyBonus;
            character.PassivePerception = rebuilt.PassivePerception;
            character.Notes = rebuilt.Notes;
            character.AbilitiesJson = rebuilt.AbilitiesJson;
            character.BaseAbilitiesJson = rebuilt.BaseAbilitiesJson;
            character.BonusAbilitySelectionsJson = rebuilt.BonusAbilitySelectionsJson;
            character.SkillsJson = rebuilt.SkillsJson;
            character.KnownSpellsJson = rebuilt.KnownSpellsJson;
            character.SpellSlotsJson = rebuilt.SpellSlotsJson;
            character.SpentSpellSlotsJson = character.SpentSpellSlotsJson;
            character.InventoryJson = rebuilt.InventoryJson;
            character.ComputedSnapshotJson = rebuilt.ComputedSnapshotJson;
            character.CalculationTraceJson = rebuilt.CalculationTraceJson;
            character.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(character.ToDto(canEdit: isGameMaster || isOwner));
        }).RequireAuthorization();

        endpoints.MapPost("/api/characters/{id:guid}/rest", async (
            Guid id,
            CharacterRestRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            CharacterRestService restService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var character = await dbContext.Characters.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (character is null)
            {
                return Results.NotFound();
            }

            var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
            var isOwner = character.OwnerUserId == user.Id;
            if (!isGameMaster && !isOwner)
            {
                return Results.Forbid();
            }

            var restResult = restService.ApplyRest(character, request, canEdit: isOwner || isGameMaster);
            if (!restResult.IsSuccess)
            {
                return Results.ValidationProblem(restResult.Errors!);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(restResult.Result);
        }).RequireAuthorization();

        endpoints.MapPost("/api/characters/{id:guid}/cast-spell", async (
            Guid id,
            CharacterCastSpellRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AppDbContext dbContext,
            CharacterSpellService spellService,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var character = await dbContext.Characters.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (character is null)
            {
                return Results.NotFound();
            }

            var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
            var isOwner = character.OwnerUserId == user.Id;
            if (!isGameMaster && !isOwner)
            {
                return Results.Forbid();
            }

            var spellResult = await spellService.CastAsync(character, request, cancellationToken);
            if (spellResult.IsNotFound)
            {
                return Results.NotFound();
            }

            if (!spellResult.IsSuccess)
            {
                return Results.ValidationProblem(spellResult.Errors!);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.Ok(spellResult.Result);
        }).RequireAuthorization();

        return endpoints;
    }
}

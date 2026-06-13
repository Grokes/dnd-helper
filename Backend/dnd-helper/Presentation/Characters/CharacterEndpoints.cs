using dnd_helper.Infrastructure.Persistence.Postgres;
using dnd_helper.Presentation.Common;
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
            CreateCharacterUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(request, user.Id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapPut("/api/characters/{id:guid}", async (
            Guid id,
            UpdateCharacterRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            UpdateCharacterUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
            var result = await useCase.ExecuteAsync(id, request, user.Id, isGameMaster, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapPost("/api/characters/{id:guid}/rest", async (
            Guid id,
            CharacterRestRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            RestCharacterUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
            var result = await useCase.ExecuteAsync(id, request, user.Id, isGameMaster, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapPost("/api/characters/{id:guid}/cast-spell", async (
            Guid id,
            CharacterCastSpellRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            CastCharacterSpellUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var isGameMaster = await userManager.IsInRoleAsync(user, ApplicationRoles.GameMaster);
            var result = await useCase.ExecuteAsync(id, request, user.Id, isGameMaster, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        return endpoints;
    }
}

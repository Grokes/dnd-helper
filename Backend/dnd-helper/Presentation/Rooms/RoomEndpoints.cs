using dnd_helper.Application.Rooms.UseCases;
using dnd_helper.Presentation.Common;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace dnd_helper.Presentation.Rooms;

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/rooms", async (
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            ListRoomsUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(user.Id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms", async (
            CreateRoomRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            CreateRoomUseCase useCase,
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

        endpoints.MapGet("/api/rooms/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            GetRoomUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(id, user.Id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/join", async (
            JoinRoomRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            JoinRoomUseCase useCase,
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

        endpoints.MapPost("/api/rooms/join/invite", async (
            JoinRoomByInviteRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            JoinRoomByInviteUseCase useCase,
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

        endpoints.MapPut("/api/rooms/{id:guid}/character", async (
            Guid id,
            SelectRoomCharacterRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            SelectRoomCharacterUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(id, request, user.Id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/presence", async (
            Guid id,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            UpdateRoomPresenceUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(id, user.Id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapPut("/api/rooms/{id:guid}/members/{memberUserId}/role", async (
            Guid id,
            string memberUserId,
            UpdateRoomMemberRoleRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            UpdateRoomMemberRoleUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(id, memberUserId, request, user.Id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/combat/start", async (
            Guid id,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            StartRoomCombatUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(id, user.Id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/combat/end", async (
            Guid id,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            EndRoomCombatUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(id, user.Id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/combat/turn/end", async (
            Guid id,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            FinishRoomTurnUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(id, user.Id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapGet("/api/rooms/{id:guid}/monsters", async (
            Guid id,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            ListRoomMonstersUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(id, user.Id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/monsters", async (
            Guid id,
            AddRoomMonsterRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AddRoomMonsterUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(id, request, user.Id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/monsters/{monsterId:guid}/damage", async (
            Guid id,
            Guid monsterId,
            ApplyMonsterDamageRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            ApplyRoomMonsterDamageUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(id, monsterId, request, user.Id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapDelete("/api/rooms/{id:guid}/monsters/{monsterId:guid}", async (
            Guid id,
            Guid monsterId,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            DeleteRoomMonsterUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(id, monsterId, user.Id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/monsters/{monsterId:guid}/attack", async (
            Guid id,
            Guid monsterId,
            MonsterAttackRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            AttackWithRoomMonsterUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(id, monsterId, request, user.Id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        endpoints.MapPost("/api/rooms/{id:guid}/monsters/{monsterId:guid}/roll-damage", async (
            Guid id,
            Guid monsterId,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager,
            RollRoomMonsterDamageUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await useCase.ExecuteAsync(id, monsterId, user.Id, cancellationToken);
            return result.ToHttpResult();
        }).RequireAuthorization();

        return endpoints;
    }

}

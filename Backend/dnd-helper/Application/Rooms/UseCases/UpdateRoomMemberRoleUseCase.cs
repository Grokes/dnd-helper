using dnd_helper.Application.Common.UseCases;
using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Application.Rooms.UseCases;

public sealed class UpdateRoomMemberRoleUseCase
{
    private readonly AppDbContext dbContext;

    public UpdateRoomMemberRoleUseCase(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<UseCaseResult<RoomDto>> ExecuteAsync(
        Guid roomId,
        string memberUserId,
        UpdateRoomMemberRoleRequest request,
        string currentUserId,
        CancellationToken cancellationToken)
    {
        var currentMembership = await dbContext.RoomMemberships
            .Include(member => member.Room)
            .FirstOrDefaultAsync(member => member.RoomId == roomId && member.UserId == currentUserId, cancellationToken);

        if (currentMembership?.Room is null)
        {
            return UseCaseResult<RoomDto>.Forbidden();
        }

        if (currentMembership.Role != RoomMemberRoles.GameMaster && currentMembership.Room.OwnerUserId != currentUserId)
        {
            return UseCaseResult<RoomDto>.Forbidden();
        }

        var normalizedRole = NormalizeRoomRole(request.Role);
        if (normalizedRole is null)
        {
            return UseCaseResult<RoomDto>.ValidationFailed(new Dictionary<string, string[]>
            {
                ["role"] = ["Роль комнаты должна быть GameMaster или Player."]
            });
        }

        var targetMembership = await dbContext.RoomMemberships.FirstOrDefaultAsync(
            member => member.RoomId == roomId && member.UserId == memberUserId,
            cancellationToken);
        if (targetMembership is null)
        {
            return UseCaseResult<RoomDto>.NotFound();
        }

        targetMembership.Role = normalizedRole;
        await dbContext.SaveChangesAsync(cancellationToken);

        var room = await dbContext.Rooms
            .AsNoTracking()
            .IncludeRoomGraph()
            .FirstAsync(existingRoom => existingRoom.Id == roomId, cancellationToken);
        return UseCaseResult<RoomDto>.Success(room.ToDto(currentUserId));
    }

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

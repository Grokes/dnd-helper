using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Application.Rooms;

public sealed class RoomAccessService
{
    private readonly AppDbContext dbContext;

    public RoomAccessService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<RoomMembershipEntity?> GetMembershipAsync(
        Guid roomId,
        string userId,
        CancellationToken cancellationToken)
    {
        return dbContext.RoomMemberships
            .Include(member => member.Room)
            .FirstOrDefaultAsync(member => member.RoomId == roomId && member.UserId == userId, cancellationToken);
    }

    public async Task<RoomMembershipEntity?> GetManagerMembershipAsync(
        Guid roomId,
        string userId,
        CancellationToken cancellationToken)
    {
        var membership = await GetMembershipAsync(roomId, userId, cancellationToken);
        return CanManageRoom(membership, userId) ? membership : null;
    }

    public static bool CanManageRoom(RoomMembershipEntity? membership, string userId)
    {
        if (membership?.Room is null)
        {
            return false;
        }

        return membership.Role == RoomMemberRoles.GameMaster || membership.Room.OwnerUserId == userId;
    }
}

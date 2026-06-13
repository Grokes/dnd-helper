using dnd_helper.Application.Common.UseCases;
using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Application.Rooms.UseCases;

public sealed class UpdateRoomPresenceUseCase
{
    private readonly AppDbContext dbContext;

    public UpdateRoomPresenceUseCase(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<UseCaseResult<object>> ExecuteAsync(
        Guid roomId,
        string userId,
        CancellationToken cancellationToken)
    {
        var membership = await dbContext.RoomMemberships.FirstOrDefaultAsync(
            member => member.RoomId == roomId && member.UserId == userId,
            cancellationToken);

        if (membership is null)
        {
            return UseCaseResult<object>.Forbidden();
        }

        membership.LastSeenAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return UseCaseResult<object>.NoContent();
    }
}

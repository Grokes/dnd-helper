using dnd_helper.Application.Common.UseCases;
using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Application.Rooms.UseCases;

public sealed class GetRoomUseCase
{
    private readonly AppDbContext dbContext;

    public GetRoomUseCase(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<UseCaseResult<RoomDto>> ExecuteAsync(
        Guid roomId,
        string userId,
        CancellationToken cancellationToken)
    {
        var room = await dbContext.Rooms
            .AsNoTracking()
            .IncludeRoomGraph()
            .FirstOrDefaultAsync(existingRoom => existingRoom.Id == roomId, cancellationToken);

        if (room is null)
        {
            return UseCaseResult<RoomDto>.NotFound();
        }

        if (!room.Members.Any(member => member.UserId == userId))
        {
            return UseCaseResult<RoomDto>.Forbidden();
        }

        return UseCaseResult<RoomDto>.Success(room.ToDto(userId));
    }
}

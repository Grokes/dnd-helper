using dnd_helper.Application.Common.UseCases;
using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Application.Rooms.UseCases;

public sealed class ListRoomsUseCase
{
    private readonly AppDbContext dbContext;

    public ListRoomsUseCase(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<UseCaseResult<IReadOnlyList<RoomSummaryDto>>> ExecuteAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var rooms = await dbContext.Rooms
            .AsNoTracking()
            .IncludeRoomGraph()
            .Where(room => room.Members.Any(member => member.UserId == userId))
            .OrderBy(room => room.Name)
            .ToListAsync(cancellationToken);

        return UseCaseResult<IReadOnlyList<RoomSummaryDto>>.Success(
            rooms.Select(room => room.ToSummaryDto(userId)).ToList());
    }
}

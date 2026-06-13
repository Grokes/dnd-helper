using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Application.Rooms.UseCases;

internal static class RoomUseCaseQueries
{
    public static IQueryable<RoomEntity> IncludeRoomGraph(this IQueryable<RoomEntity> query)
    {
        return query
            .Include(room => room.OwnerUser)
            .Include(room => room.Members).ThenInclude(member => member.User)
            .Include(room => room.Members).ThenInclude(member => member.Characters).ThenInclude(link => link.Character);
    }
}

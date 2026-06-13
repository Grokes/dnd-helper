using dnd_helper.Application.Common.UseCases;
using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Application.Rooms.UseCases;

public sealed class SelectRoomCharacterUseCase
{
    private readonly AppDbContext dbContext;

    public SelectRoomCharacterUseCase(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<UseCaseResult<RoomDto>> ExecuteAsync(
        Guid roomId,
        SelectRoomCharacterRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        var membership = await dbContext.RoomMemberships
            .Include(member => member.Room)
            .Include(member => member.Characters)
            .FirstOrDefaultAsync(member => member.RoomId == roomId && member.UserId == userId, cancellationToken);

        if (membership is null)
        {
            return UseCaseResult<RoomDto>.Forbidden();
        }

        if (request.CharacterId is not null)
        {
            var character = await dbContext.Characters.FirstOrDefaultAsync(existingCharacter =>
                existingCharacter.Id == request.CharacterId.Value && existingCharacter.OwnerUserId == userId,
                cancellationToken);

            if (character is null)
            {
                return UseCaseResult<RoomDto>.ValidationFailed(new Dictionary<string, string[]>
                {
                    ["characterId"] = ["Можно выбрать только собственного персонажа."]
                });
            }

            if (!membership.Characters.Any(link => link.CharacterId == character.Id))
            {
                membership.Characters.Add(new RoomMembershipCharacterEntity
                {
                    RoomId = roomId,
                    UserId = userId,
                    CharacterId = character.Id
                });
            }
        }
        else
        {
            membership.Characters.Clear();
        }

        membership.LastSeenAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var room = await dbContext.Rooms
            .AsNoTracking()
            .IncludeRoomGraph()
            .FirstAsync(existingRoom => existingRoom.Id == roomId, cancellationToken);
        return UseCaseResult<RoomDto>.Success(room.ToDto(userId));
    }
}

using dnd_helper.Application.Common.UseCases;
using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Application.Rooms.UseCases;

public sealed class JoinRoomUseCase
{
    private readonly AppDbContext dbContext;

    public JoinRoomUseCase(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<UseCaseResult<RoomDto>> ExecuteAsync(
        JoinRoomRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.JoinCode))
        {
            return UseCaseResult<RoomDto>.ValidationFailed(new Dictionary<string, string[]>
            {
                ["joinCode"] = ["Укажите код комнаты."]
            });
        }

        var joinCode = request.JoinCode.Trim().ToUpperInvariant();
        var room = await dbContext.Rooms
            .IncludeRoomGraph()
            .FirstOrDefaultAsync(existingRoom => existingRoom.JoinCode == joinCode, cancellationToken);

        if (room is null)
        {
            return UseCaseResult<RoomDto>.ValidationFailed(new Dictionary<string, string[]>
            {
                ["joinCode"] = ["Комната с таким кодом не найдена."]
            });
        }

        AddPlayerMembershipIfMissing(room, userId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return UseCaseResult<RoomDto>.Success(room.ToDto(userId));
    }

    internal static void AddPlayerMembershipIfMissing(RoomEntity room, string userId)
    {
        if (room.Members.Any(member => member.UserId == userId))
        {
            return;
        }

        room.Members.Add(new RoomMembershipEntity
        {
            RoomId = room.Id,
            UserId = userId,
            Role = RoomMemberRoles.Player,
            InventoryJson = "[]",
            JoinedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow
        });
    }
}

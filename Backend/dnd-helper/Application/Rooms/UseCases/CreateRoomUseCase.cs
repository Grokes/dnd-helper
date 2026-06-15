using dnd_helper.Application.Common.UseCases;
using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Application.Rooms.UseCases;

public sealed class CreateRoomUseCase
{
    private readonly AppDbContext dbContext;

    public CreateRoomUseCase(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<UseCaseResult<RoomDto>> ExecuteAsync(
        CreateRoomRequest request,
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return UseCaseResult<RoomDto>.ValidationFailed(new Dictionary<string, string[]>
            {
                ["name"] = ["У комнаты должно быть название."]
            });
        }

        var now = DateTime.UtcNow;
        var room = new RoomEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            JoinCode = GenerateJoinCode(),
            InviteToken = GenerateInviteToken(),
            OwnerUserId = ownerUserId,
            CreatedAtUtc = now,
            Members =
            [
                new RoomMembershipEntity
                {
                    UserId = ownerUserId,
                    Role = RoomMemberRoles.GameMaster,
                    JoinedAtUtc = now,
                    LastSeenAtUtc = now
                }
            ]
        };

        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync(cancellationToken);

        var hydratedRoom = await dbContext.Rooms
            .AsNoTracking()
            .IncludeRoomGraph()
            .FirstAsync(existingRoom => existingRoom.Id == room.Id, cancellationToken);

        return UseCaseResult<RoomDto>.Created(hydratedRoom.ToDto(ownerUserId), $"/api/rooms/{room.Id}");
    }

    private static string GenerateJoinCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = Random.Shared;
        return new string(Enumerable.Range(0, 6).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray());
    }

    private static string GenerateInviteToken() => Guid.NewGuid().ToString("N");
}

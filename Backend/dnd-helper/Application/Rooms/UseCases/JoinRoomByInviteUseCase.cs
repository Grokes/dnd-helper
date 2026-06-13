using dnd_helper.Application.Common.UseCases;
using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Application.Rooms.UseCases;

public sealed class JoinRoomByInviteUseCase
{
    private readonly AppDbContext dbContext;

    public JoinRoomByInviteUseCase(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<UseCaseResult<RoomDto>> ExecuteAsync(
        JoinRoomByInviteRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.InviteToken))
        {
            return UseCaseResult<RoomDto>.ValidationFailed(new Dictionary<string, string[]>
            {
                ["inviteToken"] = ["Укажите ссылку-приглашение."]
            });
        }

        var inviteToken = request.InviteToken.Trim();
        var room = await dbContext.Rooms
            .IncludeRoomGraph()
            .FirstOrDefaultAsync(existingRoom => existingRoom.InviteToken == inviteToken, cancellationToken);

        if (room is null)
        {
            return UseCaseResult<RoomDto>.ValidationFailed(new Dictionary<string, string[]>
            {
                ["inviteToken"] = ["Приглашение недействительно или уже устарело."]
            });
        }

        JoinRoomUseCase.AddPlayerMembershipIfMissing(room, userId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return UseCaseResult<RoomDto>.Success(room.ToDto(userId));
    }
}

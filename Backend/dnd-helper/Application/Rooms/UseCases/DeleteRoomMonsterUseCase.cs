using dnd_helper.Application.Common.UseCases;

namespace dnd_helper.Application.Rooms.UseCases;

public sealed class DeleteRoomMonsterUseCase
{
    private readonly RoomAccessService roomAccessService;
    private readonly RoomMonsterService monsterService;

    public DeleteRoomMonsterUseCase(RoomAccessService roomAccessService, RoomMonsterService monsterService)
    {
        this.roomAccessService = roomAccessService;
        this.monsterService = monsterService;
    }

    public async Task<UseCaseResult<object>> ExecuteAsync(
        Guid roomId,
        Guid monsterId,
        string userId,
        CancellationToken cancellationToken)
    {
        var membership = await roomAccessService.GetManagerMembershipAsync(roomId, userId, cancellationToken);
        if (membership is null)
        {
            return UseCaseResult<object>.Forbidden();
        }

        var deleted = await monsterService.DeleteMonsterAsync(roomId, monsterId, cancellationToken);
        return deleted ? UseCaseResult<object>.NoContent() : UseCaseResult<object>.NotFound();
    }
}

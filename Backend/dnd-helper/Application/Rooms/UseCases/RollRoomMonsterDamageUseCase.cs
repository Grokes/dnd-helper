using dnd_helper.Application.Common.UseCases;

namespace dnd_helper.Application.Rooms.UseCases;

public sealed class RollRoomMonsterDamageUseCase
{
    private readonly RoomAccessService roomAccessService;
    private readonly RoomMonsterService monsterService;

    public RollRoomMonsterDamageUseCase(RoomAccessService roomAccessService, RoomMonsterService monsterService)
    {
        this.roomAccessService = roomAccessService;
        this.monsterService = monsterService;
    }

    public async Task<UseCaseResult<MonsterDamageRollDto>> ExecuteAsync(
        Guid roomId,
        Guid monsterId,
        string userId,
        CancellationToken cancellationToken)
    {
        var membership = await roomAccessService.GetManagerMembershipAsync(roomId, userId, cancellationToken);
        if (membership is null)
        {
            return UseCaseResult<MonsterDamageRollDto>.Forbidden();
        }

        var result = await monsterService.RollDamageAsync(roomId, monsterId, cancellationToken);
        return result.ToUseCaseResult();
    }
}

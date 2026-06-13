using dnd_helper.Application.Common.UseCases;

namespace dnd_helper.Application.Rooms.UseCases;

public sealed class AttackWithRoomMonsterUseCase
{
    private readonly RoomAccessService roomAccessService;
    private readonly RoomMonsterService monsterService;

    public AttackWithRoomMonsterUseCase(RoomAccessService roomAccessService, RoomMonsterService monsterService)
    {
        this.roomAccessService = roomAccessService;
        this.monsterService = monsterService;
    }

    public async Task<UseCaseResult<MonsterAttackResultDto>> ExecuteAsync(
        Guid roomId,
        Guid monsterId,
        MonsterAttackRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        var membership = await roomAccessService.GetManagerMembershipAsync(roomId, userId, cancellationToken);
        if (membership is null)
        {
            return UseCaseResult<MonsterAttackResultDto>.Forbidden();
        }

        var result = await monsterService.AttackAsync(roomId, monsterId, request, cancellationToken);
        return result.ToUseCaseResult();
    }
}

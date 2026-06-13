using dnd_helper.Application.Common.UseCases;

namespace dnd_helper.Application.Rooms.UseCases;

public sealed class ApplyRoomMonsterDamageUseCase
{
    private readonly RoomAccessService roomAccessService;
    private readonly RoomMonsterService monsterService;

    public ApplyRoomMonsterDamageUseCase(RoomAccessService roomAccessService, RoomMonsterService monsterService)
    {
        this.roomAccessService = roomAccessService;
        this.monsterService = monsterService;
    }

    public async Task<UseCaseResult<RoomMonsterDamageResultDto>> ExecuteAsync(
        Guid roomId,
        Guid monsterId,
        ApplyMonsterDamageRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        var membership = await roomAccessService.GetManagerMembershipAsync(roomId, userId, cancellationToken);
        if (membership is null)
        {
            return UseCaseResult<RoomMonsterDamageResultDto>.Forbidden();
        }

        var result = await monsterService.ApplyDamageAsync(roomId, monsterId, request, cancellationToken);
        return result.ToUseCaseResult();
    }
}

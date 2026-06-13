using dnd_helper.Application.Common.UseCases;

namespace dnd_helper.Application.Rooms.UseCases;

public sealed class AddRoomMonsterUseCase
{
    private readonly RoomAccessService roomAccessService;
    private readonly RoomMonsterService monsterService;

    public AddRoomMonsterUseCase(RoomAccessService roomAccessService, RoomMonsterService monsterService)
    {
        this.roomAccessService = roomAccessService;
        this.monsterService = monsterService;
    }

    public async Task<UseCaseResult<RoomMonsterDto>> ExecuteAsync(
        Guid roomId,
        AddRoomMonsterRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        var membership = await roomAccessService.GetManagerMembershipAsync(roomId, userId, cancellationToken);
        if (membership is null)
        {
            return UseCaseResult<RoomMonsterDto>.Forbidden();
        }

        var result = await monsterService.AddMonsterAsync(roomId, request, cancellationToken);
        return result.ToUseCaseResult();
    }
}

namespace dnd_helper.Application.Rooms.UseCases;

public sealed class StartRoomCombatUseCase
{
    private readonly RoomAccessService roomAccessService;
    private readonly RoomInitiativeService initiativeService;

    public StartRoomCombatUseCase(RoomAccessService roomAccessService, RoomInitiativeService initiativeService)
    {
        this.roomAccessService = roomAccessService;
        this.initiativeService = initiativeService;
    }

    public async Task<UseCaseResult<RoomDto>> ExecuteAsync(
        Guid roomId,
        string userId,
        CancellationToken cancellationToken)
    {
        var membership = await roomAccessService.GetManagerMembershipAsync(roomId, userId, cancellationToken);
        if (membership is null)
        {
            return UseCaseResult<RoomDto>.Forbidden();
        }

        return await initiativeService.StartCombatAsync(roomId, userId, cancellationToken);
    }
}

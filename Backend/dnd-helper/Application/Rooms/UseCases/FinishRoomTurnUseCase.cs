namespace dnd_helper.Application.Rooms.UseCases;

public sealed class FinishRoomTurnUseCase
{
    private readonly RoomAccessService roomAccessService;
    private readonly RoomInitiativeService initiativeService;

    public FinishRoomTurnUseCase(RoomAccessService roomAccessService, RoomInitiativeService initiativeService)
    {
        this.roomAccessService = roomAccessService;
        this.initiativeService = initiativeService;
    }

    public async Task<UseCaseResult<RoomDto>> ExecuteAsync(
        Guid roomId,
        string userId,
        CancellationToken cancellationToken)
    {
        var membership = await roomAccessService.GetMembershipAsync(roomId, userId, cancellationToken);
        if (membership is null)
        {
            return UseCaseResult<RoomDto>.Forbidden();
        }

        return await initiativeService.FinishCurrentTurnAsync(
            roomId,
            userId,
            RoomAccessService.CanManageRoom(membership, userId),
            cancellationToken);
    }
}

using dnd_helper.Application.Common.UseCases;
using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Application.Rooms.UseCases;

public sealed class ListRoomMonstersUseCase
{
    private readonly AppDbContext dbContext;
    private readonly RoomAccessService roomAccessService;

    public ListRoomMonstersUseCase(AppDbContext dbContext, RoomAccessService roomAccessService)
    {
        this.dbContext = dbContext;
        this.roomAccessService = roomAccessService;
    }

    public async Task<UseCaseResult<IReadOnlyList<RoomMonsterDto>>> ExecuteAsync(
        Guid roomId,
        string userId,
        CancellationToken cancellationToken)
    {
        var membership = await roomAccessService.GetMembershipAsync(roomId, userId, cancellationToken);
        if (membership is null)
        {
            return UseCaseResult<IReadOnlyList<RoomMonsterDto>>.Forbidden();
        }

        var monsters = await dbContext.EncounterCombatants
            .AsNoTracking()
            .Where(combatant => combatant.Encounter!.RoomId == roomId && !combatant.IsPlayerCharacter && combatant.MonsterSlug != null)
            .Include(combatant => combatant.Encounter)
            .OrderBy(combatant => combatant.Name)
            .ToListAsync(cancellationToken);

        return UseCaseResult<IReadOnlyList<RoomMonsterDto>>.Success(
            monsters.Select(RoomMonsterService.MapMonsterDto).ToList());
    }
}

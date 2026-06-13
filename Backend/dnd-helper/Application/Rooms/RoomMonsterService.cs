using dnd_helper.Infrastructure.Persistence.Postgres;
using dnd_helper.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;

namespace dnd_helper.Application.Rooms;

public sealed class RoomMonsterService
{
    private readonly AppDbContext dbContext;
    private readonly IRulesCatalogRepository rulesRepository;
    private readonly RoomCombatService combatService;

    public RoomMonsterService(AppDbContext dbContext, IRulesCatalogRepository rulesRepository, RoomCombatService combatService)
    {
        this.dbContext = dbContext;
        this.rulesRepository = rulesRepository;
        this.combatService = combatService;
    }

    public async Task<RoomMonsterServiceOutcome<RoomMonsterDto>> AddMonsterAsync(
        Guid roomId,
        AddRoomMonsterRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.MonsterSlug))
        {
            return RoomMonsterServiceOutcome<RoomMonsterDto>.Validation("monsterSlug", "Выбери чудовище из справочника.");
        }

        var monstersCatalog = await rulesRepository.GetMonstersAsync(RulesDatabaseSeeder.RulesetId, cancellationToken);
        var monster = monstersCatalog.FirstOrDefault(item =>
            item.Slug.Equals(request.MonsterSlug.Trim(), StringComparison.OrdinalIgnoreCase));

        if (monster is null)
        {
            return RoomMonsterServiceOutcome<RoomMonsterDto>.Validation("monsterSlug", "Такого чудовища нет в справочнике PHB.");
        }

        var encounter = await dbContext.Encounters
            .Include(existingEncounter => existingEncounter.Combatants)
            .FirstOrDefaultAsync(existingEncounter => existingEncounter.RoomId == roomId, cancellationToken);

        if (encounter is null)
        {
            encounter = new EncounterEntity
            {
                Id = Guid.NewGuid(),
                RoomId = roomId,
                Name = "Основная сцена",
                CreatedAtUtc = DateTime.UtcNow
            };
            dbContext.Encounters.Add(encounter);
        }

        var combatant = new EncounterCombatantEntity
        {
            Id = Guid.NewGuid(),
            EncounterId = encounter.Id,
            MonsterSlug = monster.Slug,
            Name = monster.Name,
            ChallengeRating = monster.ChallengeRating,
            Initiative = 0,
            ArmorClass = monster.ArmorClass,
            MaxHitPoints = monster.HitPoints,
            CurrentHitPoints = monster.HitPoints,
            AttackName = string.IsNullOrWhiteSpace(monster.AttackName) ? "Атака" : monster.AttackName,
            AttackBonus = monster.AttackBonus,
            DamageDice = string.IsNullOrWhiteSpace(monster.DamageDice) ? "1d4" : monster.DamageDice,
            DamageBonus = monster.DamageBonus,
            DamageType = string.IsNullOrWhiteSpace(monster.DamageType) ? "bludgeoning" : monster.DamageType,
            IsPlayerCharacter = false
        };

        dbContext.EncounterCombatants.Add(combatant);
        await dbContext.SaveChangesAsync(cancellationToken);
        return RoomMonsterServiceOutcome<RoomMonsterDto>.Success(MapMonsterDto(combatant));
    }

    public async Task<RoomMonsterServiceOutcome<RoomMonsterDamageResultDto>> ApplyDamageAsync(
        Guid roomId,
        Guid monsterId,
        ApplyMonsterDamageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Damage <= 0)
        {
            return RoomMonsterServiceOutcome<RoomMonsterDamageResultDto>.Validation("damage", "Урон должен быть больше 0.");
        }

        var combatant = await FindMonsterCombatantAsync(roomId, monsterId, cancellationToken);
        if (combatant is null)
        {
            return RoomMonsterServiceOutcome<RoomMonsterDamageResultDto>.NotFound();
        }

        combatant.CurrentHitPoints = Math.Max(0, combatant.CurrentHitPoints - request.Damage);
        if (combatant.CurrentHitPoints <= 0)
        {
            var removedMonsterId = combatant.Id;
            var removedMonsterName = combatant.Name;
            dbContext.EncounterCombatants.Remove(combatant);
            await dbContext.SaveChangesAsync(cancellationToken);
            return RoomMonsterServiceOutcome<RoomMonsterDamageResultDto>.Success(new RoomMonsterDamageResultDto(
                removedMonsterId,
                removedMonsterName,
                true,
                null));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return RoomMonsterServiceOutcome<RoomMonsterDamageResultDto>.Success(new RoomMonsterDamageResultDto(
            combatant.Id,
            combatant.Name,
            false,
            MapMonsterDto(combatant)));
    }

    public async Task<bool> DeleteMonsterAsync(Guid roomId, Guid monsterId, CancellationToken cancellationToken)
    {
        var combatant = await FindMonsterCombatantAsync(roomId, monsterId, cancellationToken);
        if (combatant is null)
        {
            return false;
        }

        dbContext.EncounterCombatants.Remove(combatant);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RoomMonsterServiceOutcome<MonsterAttackResultDto>> AttackAsync(
        Guid roomId,
        Guid monsterId,
        MonsterAttackRequest request,
        CancellationToken cancellationToken)
    {
        var combatant = await FindMonsterCombatantAsync(roomId, monsterId, cancellationToken);
        if (combatant is null)
        {
            return RoomMonsterServiceOutcome<MonsterAttackResultDto>.NotFound();
        }

        var targetLink = await dbContext.RoomMembershipCharacters
            .Include(link => link.Character)
            .FirstOrDefaultAsync(link =>
                link.RoomId == roomId &&
                link.CharacterId == request.TargetCharacterId,
                cancellationToken);

        if (targetLink?.Character is null)
        {
            return RoomMonsterServiceOutcome<MonsterAttackResultDto>.Validation(
                "targetCharacterId",
                "Выбери персонажа, который участвует в комнате.");
        }

        var target = targetLink.Character;
        var attackResult = combatService.AttackCharacter(combatant, target);
        if (!attackResult.IsSuccess)
        {
            return RoomMonsterServiceOutcome<MonsterAttackResultDto>.Validation(attackResult.Errors!);
        }

        var computation = attackResult.Result!;
        var targetMaxHitPoints = computation.TargetMaxHitPoints;
        if (target.MaxHitPoints <= 0)
        {
            target.CurrentHitPoints = targetMaxHitPoints;
        }

        target.CurrentHitPoints = computation.NextTargetCurrentHitPoints;
        target.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return RoomMonsterServiceOutcome<MonsterAttackResultDto>.Success(computation.Result);
    }

    public async Task<RoomMonsterServiceOutcome<MonsterDamageRollDto>> RollDamageAsync(
        Guid roomId,
        Guid monsterId,
        CancellationToken cancellationToken)
    {
        var combatant = await FindMonsterCombatantAsync(roomId, monsterId, cancellationToken);
        if (combatant is null)
        {
            return RoomMonsterServiceOutcome<MonsterDamageRollDto>.NotFound();
        }

        var damageResult = combatService.RollMonsterDamage(combatant);
        if (!damageResult.IsSuccess)
        {
            return RoomMonsterServiceOutcome<MonsterDamageRollDto>.Validation(damageResult.Errors!);
        }

        return RoomMonsterServiceOutcome<MonsterDamageRollDto>.Success(damageResult.Result!);
    }

    public static RoomMonsterDto MapMonsterDto(EncounterCombatantEntity combatant)
    {
        return new RoomMonsterDto(
            combatant.Id,
            combatant.MonsterSlug ?? string.Empty,
            combatant.Name,
            combatant.ChallengeRating,
            combatant.ArmorClass,
            combatant.MaxHitPoints,
            combatant.CurrentHitPoints,
            combatant.AttackName ?? "Атака",
            combatant.AttackBonus,
            combatant.DamageDice ?? "1d4",
            combatant.DamageBonus,
            combatant.DamageType ?? "bludgeoning");
    }

    private Task<EncounterCombatantEntity?> FindMonsterCombatantAsync(Guid roomId, Guid monsterId, CancellationToken cancellationToken)
    {
        return dbContext.EncounterCombatants
            .Include(existingCombatant => existingCombatant.Encounter)
            .FirstOrDefaultAsync(existingCombatant =>
                existingCombatant.Id == monsterId &&
                existingCombatant.Encounter!.RoomId == roomId &&
                !existingCombatant.IsPlayerCharacter,
                cancellationToken);
    }
}

public sealed record RoomMonsterServiceOutcome<T>(
    T? Result,
    Dictionary<string, string[]>? Errors,
    bool IsNotFound)
{
    public bool IsSuccess => Result is not null && Errors is null && !IsNotFound;

    public static RoomMonsterServiceOutcome<T> Success(T result) => new(result, null, false);

    public static RoomMonsterServiceOutcome<T> Validation(string key, string message) => new(
        default,
        new Dictionary<string, string[]> { [key] = [message] },
        false);

    public static RoomMonsterServiceOutcome<T> Validation(Dictionary<string, string[]> errors) => new(
        default,
        errors,
        false);

    public static RoomMonsterServiceOutcome<T> NotFound() => new(default, null, true);
}

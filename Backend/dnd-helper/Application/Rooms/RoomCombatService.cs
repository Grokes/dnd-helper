namespace dnd_helper.Application.Rooms;

public sealed class RoomCombatService
{
    private readonly DiceRoller diceRoller;

    public RoomCombatService(DiceRoller diceRoller)
    {
        this.diceRoller = diceRoller;
    }

    public RoomCombatOutcome<MonsterDamageRollDto> RollMonsterDamage(EncounterCombatantEntity monster)
    {
        if (!diceRoller.TryRoll(monster.DamageDice ?? "1d4", out var damageRoll))
        {
            return RoomCombatOutcome<MonsterDamageRollDto>.Validation(
                "damageDice",
                "Невозможно бросить урон: некорректный формат кости урона.");
        }

        var total = Math.Max(0, damageRoll.Total + monster.DamageBonus);
        return RoomCombatOutcome<MonsterDamageRollDto>.Success(new MonsterDamageRollDto(
            monster.Id,
            monster.Name,
            monster.AttackName ?? "Атака",
            damageRoll.Expression,
            damageRoll.Total,
            monster.DamageBonus,
            total,
            DateTime.UtcNow));
    }

    public RoomCombatOutcome<MonsterAttackComputation> AttackCharacter(
        EncounterCombatantEntity monster,
        CharacterEntity target)
    {
        var targetMaxHitPoints = target.MaxHitPoints <= 0 ? target.HitPoints : target.MaxHitPoints;
        var targetCurrentHitPoints = target.CurrentHitPoints;
        if (target.MaxHitPoints <= 0)
        {
            targetCurrentHitPoints = targetMaxHitPoints;
        }

        var attack = diceRoller.RollD20(monster.AttackBonus);
        var isHit = attack.IsNaturalTwenty || (!attack.IsNaturalOne && attack.Total >= target.ArmorClass);

        var damageExpression = "—";
        var damageDiceResult = 0;
        var damageTotal = 0;
        var nextTargetHitPoints = targetCurrentHitPoints;
        var message = $"{monster.Name} промахивается по {target.Name}.";

        if (isHit)
        {
            if (!diceRoller.TryRoll(monster.DamageDice ?? "1d4", out var damageRoll))
            {
                return RoomCombatOutcome<MonsterAttackComputation>.Validation(
                    "damageDice",
                    "Невозможно бросить урон: некорректный формат кости урона.");
            }

            damageExpression = damageRoll.Expression;
            damageDiceResult = damageRoll.Total;
            damageTotal = Math.Max(0, damageDiceResult + monster.DamageBonus);
            nextTargetHitPoints = Math.Max(0, targetCurrentHitPoints - damageTotal);
            message = $"{monster.Name} попадает по {target.Name} и наносит {damageTotal} урона.";
        }

        return RoomCombatOutcome<MonsterAttackComputation>.Success(new MonsterAttackComputation(
            new MonsterAttackResultDto(
                monster.Id,
                monster.Name,
                target.Id,
                target.Name,
                attack.Roll,
                monster.AttackBonus,
                attack.Total,
                target.ArmorClass,
                attack.IsNaturalTwenty,
                isHit,
                damageExpression,
                damageDiceResult,
                monster.DamageBonus,
                damageTotal,
                nextTargetHitPoints,
                targetMaxHitPoints,
                DateTime.UtcNow,
                message),
            nextTargetHitPoints,
            targetMaxHitPoints));
    }
}

public sealed record MonsterAttackComputation(
    MonsterAttackResultDto Result,
    int NextTargetCurrentHitPoints,
    int TargetMaxHitPoints);

public sealed record RoomCombatOutcome<T>(
    T? Result,
    Dictionary<string, string[]>? Errors)
{
    public bool IsSuccess => Result is not null && Errors is null;

    public static RoomCombatOutcome<T> Success(T result) => new(result, null);

    public static RoomCombatOutcome<T> Validation(string key, string message) => new(
        default,
        new Dictionary<string, string[]> { [key] = [message] });
}

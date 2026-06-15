namespace dnd_helper.Domain.Characters;

public sealed class EncounterEntity
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public RoomEntity? Room { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsCombatActive { get; set; }
    public int RoundNumber { get; set; }
    public Guid? CurrentTurnCombatantId { get; set; }
    public DateTime? TurnStartedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<EncounterCombatantEntity> Combatants { get; set; } = [];
}

public sealed class EncounterCombatantEntity
{
    public Guid Id { get; set; }
    public Guid EncounterId { get; set; }
    public EncounterEntity? Encounter { get; set; }
    public Guid? CharacterId { get; set; }
    public string? MonsterSlug { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal ChallengeRating { get; set; }
    public int Initiative { get; set; }
    public int ArmorClass { get; set; }
    public int MaxHitPoints { get; set; }
    public int CurrentHitPoints { get; set; }
    public string? AttackName { get; set; }
    public int AttackBonus { get; set; }
    public string? DamageDice { get; set; }
    public int DamageBonus { get; set; }
    public string? DamageType { get; set; }
    public bool IsPlayerCharacter { get; set; }
}

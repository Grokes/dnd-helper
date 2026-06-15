namespace dnd_helper.Domain.Characters;

public sealed class CharacterBaseAbilityEntity
{
    public Guid CharacterId { get; set; }
    public CharacterEntity? Character { get; set; }
    public string Ability { get; set; } = string.Empty;
    public int Score { get; set; }
}

public sealed class CharacterAbilityEntity
{
    public Guid CharacterId { get; set; }
    public CharacterEntity? Character { get; set; }
    public string Ability { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Modifier { get; set; }
}

public sealed class CharacterSelectedOptionEntity
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public CharacterEntity? Character { get; set; }
    public string Source { get; set; } = string.Empty;
    public string OptionType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public sealed class CharacterSkillProficiencyEntity
{
    public Guid CharacterId { get; set; }
    public CharacterEntity? Character { get; set; }
    public string SkillId { get; set; } = string.Empty;
    public int Bonus { get; set; }
}

public sealed class CharacterSavingThrowProficiencyEntity
{
    public Guid CharacterId { get; set; }
    public CharacterEntity? Character { get; set; }
    public string Ability { get; set; } = string.Empty;
}

public sealed class CharacterKnownSpellEntity
{
    public Guid CharacterId { get; set; }
    public CharacterEntity? Character { get; set; }
    public string SpellSlug { get; set; } = string.Empty;
}

public sealed class CharacterSpellSlotEntity
{
    public Guid CharacterId { get; set; }
    public CharacterEntity? Character { get; set; }
    public int SpellLevel { get; set; }
    public int MaxSlots { get; set; }
    public int SpentSlots { get; set; }
}

public sealed class CharacterInventoryItemEntity
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public CharacterEntity? Character { get; set; }
    public Guid? RoomId { get; set; }
    public string ItemRef { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string? EquipmentSlot { get; set; }
    public bool IsEquipped { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CharacterCalculationTraceEntryEntity
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public CharacterEntity? Character { get; set; }
    public int Order { get; set; }
    public string Target { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Operation { get; set; } = string.Empty;
}

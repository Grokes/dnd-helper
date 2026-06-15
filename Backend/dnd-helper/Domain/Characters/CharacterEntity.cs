using System.Text.Json;

namespace dnd_helper.Domain.Characters;

public sealed class CharacterEntity
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private sealed record SelectedOptionsState(
        List<string> BonusAbilitySelections,
        List<string> RaceSkillSelections,
        List<string> ClassSkillSelections);

    public Guid Id { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public ApplicationUser? OwnerUser { get; set; }
    public string RaceId { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string BackgroundId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Race { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Subclass { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Background { get; set; } = string.Empty;
    public string Alignment { get; set; } = string.Empty;
    public int ArmorClass { get; set; }
    public string WeaponDamage { get; set; } = string.Empty;
    public int HitDie { get; set; } = 8;
    public int HitPoints { get; set; }
    public int MaxHitPoints { get; set; }
    public int CurrentHitPoints { get; set; }
    public int SpentHitDice { get; set; }
    public int Speed { get; set; }
    public int ProficiencyBonus { get; set; }
    public int PassivePerception { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string AbilitiesJson { get; set; } = "[]";
    public string BaseAbilitiesJson { get; set; } = "[]";
    public string BonusAbilitySelectionsJson { get; set; } = "[]";
    public string SkillsJson { get; set; } = "[]";
    public string KnownSpellsJson { get; set; } = "[]";
    public string SpellSlotsJson { get; set; } = "[]";
    public string SpentSpellSlotsJson { get; set; } = "{}";
    public string InventoryJson { get; set; } = "[]";
    public string ComputedSnapshotJson { get; set; } = "{}";
    public string CalculationTraceJson { get; set; } = "[]";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<CharacterBaseAbilityEntity> BaseAbilities { get; set; } = [];
    public List<CharacterAbilityEntity> Abilities { get; set; } = [];
    public List<CharacterSelectedOptionEntity> SelectedOptions { get; set; } = [];
    public List<CharacterSkillProficiencyEntity> SkillProficiencies { get; set; } = [];
    public List<CharacterSavingThrowProficiencyEntity> SavingThrowProficiencies { get; set; } = [];
    public List<CharacterKnownSpellEntity> KnownSpells { get; set; } = [];
    public List<CharacterSpellSlotEntity> SpellSlots { get; set; } = [];
    public List<CharacterInventoryItemEntity> InventoryItems { get; set; } = [];
    public List<CharacterCalculationTraceEntryEntity> CalculationTraceEntries { get; set; } = [];

    public static CharacterEntity FromComputed(CharacterComputationResult computedCharacter, string ownerUserId)
    {
        var entity = new CharacterEntity
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        entity.UpdateFromComputed(computedCharacter);
        return entity;
    }

    public void UpdateFromComputed(CharacterComputationResult computedCharacter)
    {
        RaceId = computedCharacter.RaceId;
        ClassId = computedCharacter.ClassId;
        BackgroundId = computedCharacter.BackgroundId;
        Name = computedCharacter.Name;
        Race = computedCharacter.Race;
        ClassName = computedCharacter.ClassName;
        Subclass = computedCharacter.SubclassId;
        Level = computedCharacter.Level;
        Background = computedCharacter.Background;
        Alignment = computedCharacter.Alignment;
        ArmorClass = computedCharacter.ArmorClass;
        WeaponDamage = computedCharacter.WeaponDamage ?? string.Empty;
        HitDie = computedCharacter.HitDie;
        HitPoints = computedCharacter.HitPoints;
        var hadHpState = MaxHitPoints > 0;
        var previousCurrentHitPoints = CurrentHitPoints;
        MaxHitPoints = computedCharacter.HitPoints;
        CurrentHitPoints = hadHpState
            ? Math.Clamp(previousCurrentHitPoints, 0, MaxHitPoints)
            : MaxHitPoints;
        Speed = computedCharacter.Speed;
        ProficiencyBonus = computedCharacter.ProficiencyBonus;
        PassivePerception = computedCharacter.PassivePerception;
        Notes = computedCharacter.Notes;
        AbilitiesJson = JsonSerializer.Serialize(computedCharacter.Abilities, JsonOptions);
        BaseAbilitiesJson = JsonSerializer.Serialize(computedCharacter.BaseAbilities, JsonOptions);
        BonusAbilitySelectionsJson = JsonSerializer.Serialize(
            new SelectedOptionsState(
                computedCharacter.BonusAbilitySelections,
                computedCharacter.RaceSkillSelections,
                computedCharacter.ClassSkillSelections),
            JsonOptions);
        SkillsJson = JsonSerializer.Serialize(computedCharacter.Skills, JsonOptions);
        KnownSpellsJson = JsonSerializer.Serialize(computedCharacter.KnownSpells, JsonOptions);
        SpellSlotsJson = JsonSerializer.Serialize(computedCharacter.SpellSlots, JsonOptions);
        if (string.IsNullOrWhiteSpace(SpentSpellSlotsJson))
        {
            SpentSpellSlotsJson = "{}";
        }
        InventoryJson = JsonSerializer.Serialize(computedCharacter.Inventory, JsonOptions);
        ComputedSnapshotJson = JsonSerializer.Serialize(computedCharacter.ComputedSnapshot, JsonOptions);
        CalculationTraceJson = JsonSerializer.Serialize(computedCharacter.CalculationTrace, JsonOptions);
        ReplaceNormalizedState(computedCharacter);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReplaceNormalizedState(CharacterComputationResult computedCharacter)
    {
        BaseAbilities.Clear();
        BaseAbilities.AddRange(computedCharacter.BaseAbilities.Select(item => new CharacterBaseAbilityEntity
        {
            CharacterId = Id,
            Character = this,
            Ability = item.Key.ToUpperInvariant(),
            Score = item.Score
        }));

        Abilities.Clear();
        Abilities.AddRange(computedCharacter.Abilities.Select(item => new CharacterAbilityEntity
        {
            CharacterId = Id,
            Character = this,
            Ability = item.Key.ToUpperInvariant(),
            Score = item.Score,
            Modifier = item.Modifier
        }));

        SelectedOptions.Clear();
        SelectedOptions.AddRange(computedCharacter.BonusAbilitySelections
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(value => CreateSelectedOption("race", "ability-bonus", value.ToUpperInvariant())));
        SelectedOptions.AddRange(computedCharacter.RaceSkillSelections
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(value => CreateSelectedOption("race", "skill", value)));
        SelectedOptions.AddRange(computedCharacter.ClassSkillSelections
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(value => CreateSelectedOption("class", "skill", value)));

        SkillProficiencies.Clear();
        SkillProficiencies.AddRange(computedCharacter.Skills
            .GroupBy(item => item.SkillId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(item => new CharacterSkillProficiencyEntity
            {
                CharacterId = Id,
                Character = this,
                SkillId = item.SkillId,
                Bonus = item.Level
            }));

        SavingThrowProficiencies.Clear();
        SavingThrowProficiencies.AddRange(computedCharacter.SavingThrowProficiencies
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(ability => new CharacterSavingThrowProficiencyEntity
            {
                CharacterId = Id,
                Character = this,
                Ability = ability
            }));

        KnownSpells.Clear();
        KnownSpells.AddRange(computedCharacter.KnownSpells
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(spellSlug => new CharacterKnownSpellEntity
            {
                CharacterId = Id,
                Character = this,
                SpellSlug = spellSlug
            }));

        SpellSlots.Clear();
        SpellSlots.AddRange(computedCharacter.SpellSlots
            .GroupBy(item => item.SpellLevel)
            .Select(group => group.First())
            .Select(slot => new CharacterSpellSlotEntity
            {
                CharacterId = Id,
                Character = this,
                SpellLevel = slot.SpellLevel,
                MaxSlots = slot.Slots,
                SpentSlots = 0
            }));

        ReplaceInventoryItems(computedCharacter.Inventory);

        CalculationTraceEntries.Clear();
        CalculationTraceEntries.AddRange(computedCharacter.CalculationTrace
            .Select((entry, index) => new CharacterCalculationTraceEntryEntity
            {
                Id = Guid.NewGuid(),
                CharacterId = Id,
                Character = this,
                Order = index,
                Target = entry.Target,
                Source = entry.Source,
                Reason = entry.Reason,
                Value = entry.Value,
                Operation = entry.Operation
            }));
    }

    public void ReplaceNormalizedStateFrom(CharacterEntity source)
    {
        BaseAbilities.Clear();
        BaseAbilities.AddRange(source.BaseAbilities.Select(item => new CharacterBaseAbilityEntity { CharacterId = Id, Character = this, Ability = item.Ability, Score = item.Score }));

        Abilities.Clear();
        Abilities.AddRange(source.Abilities.Select(item => new CharacterAbilityEntity { CharacterId = Id, Character = this, Ability = item.Ability, Score = item.Score, Modifier = item.Modifier }));

        SelectedOptions.Clear();
        SelectedOptions.AddRange(source.SelectedOptions.Select(item => new CharacterSelectedOptionEntity { Id = Guid.NewGuid(), CharacterId = Id, Character = this, Source = item.Source, OptionType = item.OptionType, Value = item.Value }));

        SkillProficiencies.Clear();
        SkillProficiencies.AddRange(source.SkillProficiencies.Select(item => new CharacterSkillProficiencyEntity { CharacterId = Id, Character = this, SkillId = item.SkillId, Bonus = item.Bonus }));

        SavingThrowProficiencies.Clear();
        SavingThrowProficiencies.AddRange(source.SavingThrowProficiencies.Select(item => new CharacterSavingThrowProficiencyEntity { CharacterId = Id, Character = this, Ability = item.Ability }));

        KnownSpells.Clear();
        KnownSpells.AddRange(source.KnownSpells.Select(item => new CharacterKnownSpellEntity { CharacterId = Id, Character = this, SpellSlug = item.SpellSlug }));

        SpellSlots.Clear();
        SpellSlots.AddRange(source.SpellSlots.Select(item => new CharacterSpellSlotEntity { CharacterId = Id, Character = this, SpellLevel = item.SpellLevel, MaxSlots = item.MaxSlots, SpentSlots = item.SpentSlots }));

        InventoryItems.Clear();
        InventoryItems.AddRange(source.InventoryItems.Select(item => new CharacterInventoryItemEntity
        {
            Id = Guid.NewGuid(),
            CharacterId = Id,
            Character = this,
            RoomId = item.RoomId,
            ItemRef = item.ItemRef,
            Quantity = item.Quantity,
            EquipmentSlot = item.EquipmentSlot,
            IsEquipped = item.IsEquipped,
            CreatedAtUtc = item.CreatedAtUtc == default ? DateTime.UtcNow : item.CreatedAtUtc
        }));

        CalculationTraceEntries.Clear();
        CalculationTraceEntries.AddRange(source.CalculationTraceEntries.Select(item => new CharacterCalculationTraceEntryEntity
        {
            Id = Guid.NewGuid(),
            CharacterId = Id,
            Character = this,
            Order = item.Order,
            Target = item.Target,
            Source = item.Source,
            Reason = item.Reason,
            Value = item.Value,
            Operation = item.Operation
        }));
    }

    public void SyncSpellSlotSnapshotFromRows()
    {
        if (SpellSlots.Count == 0)
        {
            return;
        }

        SpellSlotsJson = JsonSerializer.Serialize(
            SpellSlots.OrderBy(item => item.SpellLevel).Select(item => new SpellSlotDto(item.SpellLevel, item.MaxSlots)).ToList(),
            JsonOptions);
        SpentSpellSlotsJson = JsonSerializer.Serialize(
            SpellSlots.Where(item => item.SpentSlots > 0).ToDictionary(item => item.SpellLevel, item => item.SpentSlots),
            JsonOptions);
    }

    public CharacterDto ToDto(bool canEdit = true)
    {
        var abilities = ReadAbilities();
        var skills = ReadSkills();
        var selectedOptions = ReadSelectedOptions(DeserializeSelectedOptions(BonusAbilitySelectionsJson));
        var maxSpellSlots = ReadMaxSpellSlots();
        var spentSpellSlots = ReadSpentSpellSlots();
        var currentSpellSlots = maxSpellSlots
            .Select(slot =>
            {
                var spent = spentSpellSlots.GetValueOrDefault(slot.SpellLevel);
                var current = Math.Max(0, slot.Slots - Math.Max(0, spent));
                return new SpellSlotDto(slot.SpellLevel, current);
            })
            .ToList();
        var characterClass = CharacterOptionsCatalog.Classes.FirstOrDefault(item => item.Id == ClassId);
        var race = CharacterOptionsCatalog.Races.FirstOrDefault(item => item.Id == RaceId);
        var background = CharacterOptionsCatalog.Backgrounds.FirstOrDefault(item => item.Id == BackgroundId);
        var savingThrows = CharacterBuilder.BuildSavingThrows(
            abilities,
            ReadSavingThrowProficiencies(characterClass?.SavingThrowProficiencies ?? []),
            ProficiencyBonus);
        var splitSkillSelections = race is not null && characterClass is not null && background is not null
            ? CharacterBuilder.SplitSkillSelections(skills.Select(item => item.SkillId).ToList(), race, characterClass, background)
            : (new List<string>(), new List<string>());
        var raceSkillSelections = selectedOptions.RaceSkillSelections.Count > 0
            ? selectedOptions.RaceSkillSelections
            : splitSkillSelections.Item1;
        var classSkillSelections = selectedOptions.ClassSkillSelections.Count > 0
            ? selectedOptions.ClassSkillSelections
            : splitSkillSelections.Item2;
        var normalizedMaxHitPoints = MaxHitPoints > 0 ? MaxHitPoints : HitPoints;
        var normalizedCurrentHitPoints = MaxHitPoints > 0
            ? Math.Clamp(CurrentHitPoints, 0, normalizedMaxHitPoints)
            : normalizedMaxHitPoints;
        var totalHitDice = Math.Max(1, Level);
        var normalizedSpentHitDice = Math.Clamp(SpentHitDice, 0, totalHitDice);
        var availableHitDice = Math.Max(0, totalHitDice - normalizedSpentHitDice);

        return new CharacterDto(
            Id,
            canEdit,
            RaceId,
            ClassId,
            BackgroundId,
            Name,
            Race,
            ClassName,
            Subclass,
            Level,
            Background,
            Alignment,
            ArmorClass,
            string.IsNullOrWhiteSpace(WeaponDamage) ? null : WeaponDamage,
            HitPoints,
            normalizedMaxHitPoints,
            normalizedCurrentHitPoints,
            normalizedSpentHitDice,
            availableHitDice,
            Speed,
            ProficiencyBonus,
            PassivePerception,
            Notes,
            ReadBaseAbilities(),
            selectedOptions.BonusAbilitySelections,
            raceSkillSelections,
            classSkillSelections,
            skills.Select(item => item.SkillId).ToList(),
            abilities,
            savingThrows,
            skills,
            currentSpellSlots,
            maxSpellSlots,
            ReadKnownSpells(),
            ReadInventory(),
            ReadCalculationTrace(),
            CreatedAtUtc,
            UpdatedAtUtc);
    }

    public CharacterSummaryDto ToSummaryDto()
    {
        var normalizedMaxHitPoints = MaxHitPoints > 0 ? MaxHitPoints : HitPoints;
        var normalizedCurrentHitPoints = MaxHitPoints > 0
            ? Math.Clamp(CurrentHitPoints, 0, normalizedMaxHitPoints)
            : normalizedMaxHitPoints;
        var totalHitDice = Math.Max(1, Level);
        var normalizedSpentHitDice = Math.Clamp(SpentHitDice, 0, totalHitDice);
        var availableHitDice = Math.Max(0, totalHitDice - normalizedSpentHitDice);

        return new CharacterSummaryDto(
            Id,
            Name,
            Race,
            ClassName,
            Subclass,
            Level,
            ArmorClass,
            string.IsNullOrWhiteSpace(WeaponDamage) ? null : WeaponDamage,
            HitPoints,
            normalizedMaxHitPoints,
            normalizedCurrentHitPoints,
            normalizedSpentHitDice,
            availableHitDice,
            PassivePerception,
            ReadSkills());
    }

    private SelectedOptionsState ReadSelectedOptions(SelectedOptionsState fallback)
    {
        if (SelectedOptions.Count == 0)
        {
            return fallback;
        }

        return new SelectedOptionsState(
            SelectedOptions
                .Where(item => item.Source.Equals("race", StringComparison.OrdinalIgnoreCase) &&
                               item.OptionType.Equals("ability-bonus", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            SelectedOptions
                .Where(item => item.Source.Equals("race", StringComparison.OrdinalIgnoreCase) &&
                               item.OptionType.Equals("skill", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            SelectedOptions
                .Where(item => item.Source.Equals("class", StringComparison.OrdinalIgnoreCase) &&
                               item.OptionType.Equals("skill", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    private List<BaseAbilityScoreDto> ReadBaseAbilities()
    {
        return BaseAbilities.Count > 0
            ? BaseAbilities.OrderBy(item => item.Ability).Select(item => new BaseAbilityScoreDto(item.Ability, item.Score)).ToList()
            : Deserialize<BaseAbilityScoreDto>(BaseAbilitiesJson);
    }

    private List<AbilityScoreDto> ReadAbilities()
    {
        return Abilities.Count > 0
            ? Abilities.OrderBy(item => item.Ability).Select(item => new AbilityScoreDto(item.Ability, item.Score, item.Modifier)).ToList()
            : Deserialize<AbilityScoreDto>(AbilitiesJson);
    }

    private List<SkillLevelDto> ReadSkills()
    {
        return SkillProficiencies.Count > 0
            ? SkillProficiencies.OrderBy(item => item.SkillId).Select(item => new SkillLevelDto(item.SkillId, item.Bonus)).ToList()
            : Deserialize<SkillLevelDto>(SkillsJson);
    }

    private List<string> ReadSavingThrowProficiencies(IReadOnlyList<string> fallback)
    {
        return SavingThrowProficiencies.Count > 0
            ? SavingThrowProficiencies
                .Select(item => item.Ability)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
            : fallback.ToList();
    }

    private List<SpellSlotDto> ReadMaxSpellSlots()
    {
        return SpellSlots.Count > 0
            ? SpellSlots.OrderBy(item => item.SpellLevel).Select(item => new SpellSlotDto(item.SpellLevel, item.MaxSlots)).ToList()
            : Deserialize<SpellSlotDto>(SpellSlotsJson);
    }

    private Dictionary<int, int> ReadSpentSpellSlots()
    {
        return SpellSlots.Count > 0
            ? SpellSlots.Where(item => item.SpentSlots > 0).ToDictionary(item => item.SpellLevel, item => item.SpentSlots)
            : DeserializeSpentSpellSlots(SpentSpellSlotsJson);
    }

    private List<string> ReadKnownSpells()
    {
        return KnownSpells.Count > 0
            ? KnownSpells.Select(item => item.SpellSlug).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            : Deserialize<string>(KnownSpellsJson);
    }

    private List<string> ReadInventory()
    {
        if (InventoryItems.Count == 0)
        {
            return Deserialize<string>(InventoryJson);
        }

        var entries = InventoryItems
            .Where(item => !item.IsEquipped)
            .OrderBy(item => item.CreatedAtUtc)
            .SelectMany(item => Enumerable.Repeat(item.ItemRef, Math.Max(1, item.Quantity)))
            .ToList();
        var equipToken = BuildEquipToken(InventoryItems);
        if (!string.IsNullOrWhiteSpace(equipToken))
        {
            entries.Add(equipToken);
        }

        return entries;
    }

    private List<CalculationTraceEntryDto> ReadCalculationTrace()
    {
        return CalculationTraceEntries.Count > 0
            ? CalculationTraceEntries
                .OrderBy(item => item.Order)
                .Select(item => new CalculationTraceEntryDto(item.Target, item.Source, item.Reason, item.Value, item.Operation))
                .ToList()
            : Deserialize<CalculationTraceEntryDto>(CalculationTraceJson);
    }

    private static List<T> Deserialize<T>(string source)
    {
        return JsonSerializer.Deserialize<List<T>>(source, JsonOptions) ?? [];
    }

    private static SelectedOptionsState DeserializeSelectedOptions(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return new SelectedOptionsState([], [], []);
        }

        try
        {
            using var document = JsonDocument.Parse(source);
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                var bonusOnly = JsonSerializer.Deserialize<List<string>>(source, JsonOptions) ?? [];
                return new SelectedOptionsState(
                    bonusOnly.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    [],
                    []);
            }

            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                var parsed = JsonSerializer.Deserialize<SelectedOptionsState>(source, JsonOptions);
                if (parsed is not null)
                {
                    return new SelectedOptionsState(
                        (parsed.BonusAbilitySelections ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                        (parsed.RaceSkillSelections ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                        (parsed.ClassSkillSelections ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
                }
            }
        }
        catch
        {
            // fallback below
        }

        return new SelectedOptionsState([], [], []);
    }

    private static Dictionary<int, int> DeserializeSpentSpellSlots(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<int, int>>(source, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private CharacterSelectedOptionEntity CreateSelectedOption(string source, string optionType, string value)
    {
        return new CharacterSelectedOptionEntity
        {
            Id = Guid.NewGuid(),
            CharacterId = Id,
            Character = this,
            Source = source,
            OptionType = optionType,
            Value = value
        };
    }

    private void ReplaceInventoryItems(IReadOnlyList<string> inventory)
    {
        InventoryItems.Clear();

        var createdAt = DateTime.UtcNow;
        InventoryItems.AddRange(inventory
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Where(item => !item.StartsWith("equip:", StringComparison.OrdinalIgnoreCase))
            .GroupBy(item => item, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CharacterInventoryItemEntity
            {
                Id = Guid.NewGuid(),
                CharacterId = Id,
                Character = this,
                ItemRef = group.Key,
                Quantity = group.Count(),
                CreatedAtUtc = createdAt
            }));

        var equipped = inventory
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Where(item => item.StartsWith("equip:", StringComparison.OrdinalIgnoreCase))
            .SelectMany(ParseEquipToken)
            .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());

        InventoryItems.AddRange(equipped.Select(item => new CharacterInventoryItemEntity
        {
            Id = Guid.NewGuid(),
            CharacterId = Id,
            Character = this,
            ItemRef = item.Value,
            Quantity = 1,
            EquipmentSlot = NormalizeEquipmentSlot(item.Key),
            IsEquipped = true,
            CreatedAtUtc = createdAt
        }));
    }

    private static Dictionary<string, string> ParseEquipToken(string raw)
    {
        if (!raw.StartsWith("equip:", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        return raw["equip:".Length..]
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Split('=', 2))
            .Where(parts => parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
            .ToDictionary(parts => NormalizeEquipmentSlot(parts[0].Trim()), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildEquipToken(IEnumerable<CharacterInventoryItemEntity> inventoryItems)
    {
        var equipped = inventoryItems
            .Where(item => item.IsEquipped && !string.IsNullOrWhiteSpace(item.EquipmentSlot) && !string.IsNullOrWhiteSpace(item.ItemRef))
            .GroupBy(item => NormalizeEquipmentSlot(item.EquipmentSlot!), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().ItemRef, StringComparer.OrdinalIgnoreCase);

        var parts = new List<string>();
        if (equipped.TryGetValue("body", out var body))
        {
            parts.Add($"body={body}");
        }
        if (equipped.TryGetValue("main", out var main))
        {
            parts.Add($"main={main}");
        }
        if (equipped.TryGetValue("off", out var off))
        {
            parts.Add($"off={off}");
        }

        return parts.Count == 0 ? string.Empty : $"equip:{string.Join(';', parts)}";
    }

    private static string NormalizeEquipmentSlot(string slot)
    {
        return slot.Trim().ToLowerInvariant() switch
        {
            "body" or "armor" => "body",
            "main" or "mainhand" or "main-hand" or "right" => "main",
            "off" or "offhand" or "off-hand" or "left" => "off",
            var value => value
        };
    }
}

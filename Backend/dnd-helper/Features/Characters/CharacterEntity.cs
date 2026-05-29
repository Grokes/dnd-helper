using System.Text.Json;
using dnd_helper.Features.Auth;
using dnd_helper.Features.ReferenceData;

namespace dnd_helper.Features.Characters;

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
    public string PreparedSpellsJson { get; set; } = "[]";
    public string InventoryJson { get; set; } = "[]";
    public string ActiveEffectsJson { get; set; } = "[]";
    public string ComputedSnapshotJson { get; set; } = "{}";
    public string CalculationTraceJson { get; set; } = "[]";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

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
        PreparedSpellsJson = JsonSerializer.Serialize(computedCharacter.PreparedSpells, JsonOptions);
        InventoryJson = JsonSerializer.Serialize(computedCharacter.Inventory, JsonOptions);
        ActiveEffectsJson = JsonSerializer.Serialize(computedCharacter.ActiveEffects, JsonOptions);
        ComputedSnapshotJson = JsonSerializer.Serialize(computedCharacter.ComputedSnapshot, JsonOptions);
        CalculationTraceJson = JsonSerializer.Serialize(computedCharacter.CalculationTrace, JsonOptions);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public CharacterDto ToDto(bool canEdit = true)
    {
        var abilities = Deserialize<AbilityScoreDto>(AbilitiesJson);
        var skills = Deserialize<SkillLevelDto>(SkillsJson);
        var selectedOptions = DeserializeSelectedOptions(BonusAbilitySelectionsJson);
        var maxSpellSlots = Deserialize<SpellSlotDto>(SpellSlotsJson);
        var spentSpellSlots = DeserializeSpentSpellSlots(SpentSpellSlotsJson);
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
            characterClass?.SavingThrowProficiencies ?? [],
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
            Deserialize<BaseAbilityScoreDto>(BaseAbilitiesJson),
            selectedOptions.BonusAbilitySelections,
            raceSkillSelections,
            classSkillSelections,
            skills.Select(item => item.SkillId).ToList(),
            abilities,
            savingThrows,
            skills,
            currentSpellSlots,
            maxSpellSlots,
            Deserialize<string>(KnownSpellsJson),
            Deserialize<string>(PreparedSpellsJson),
            Deserialize<string>(InventoryJson),
            Deserialize<string>(ActiveEffectsJson),
            Deserialize<CalculationTraceEntryDto>(CalculationTraceJson),
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
            Deserialize<SkillLevelDto>(SkillsJson));
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
}

using System.Text.Json;
using dnd_helper.Features.Auth;
using dnd_helper.Features.ReferenceData;

namespace dnd_helper.Features.Characters;

public sealed class CharacterEntity
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
    public int HitPoints { get; set; }
    public int Speed { get; set; }
    public int ProficiencyBonus { get; set; }
    public int PassivePerception { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string AbilitiesJson { get; set; } = "[]";
    public string BaseAbilitiesJson { get; set; } = "[]";
    public string BonusAbilitySelectionsJson { get; set; } = "[]";
    public string SkillsJson { get; set; } = "[]";
    public string SpellsJson { get; set; } = "[]";
    public string InventoryJson { get; set; } = "[]";
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
        Subclass = string.Empty;
        Level = computedCharacter.Level;
        Background = computedCharacter.Background;
        Alignment = computedCharacter.Alignment;
        ArmorClass = computedCharacter.ArmorClass;
        HitPoints = computedCharacter.HitPoints;
        Speed = computedCharacter.Speed;
        ProficiencyBonus = computedCharacter.ProficiencyBonus;
        PassivePerception = computedCharacter.PassivePerception;
        Notes = computedCharacter.Notes;
        AbilitiesJson = JsonSerializer.Serialize(computedCharacter.Abilities, JsonOptions);
        BaseAbilitiesJson = JsonSerializer.Serialize(computedCharacter.BaseAbilities, JsonOptions);
        BonusAbilitySelectionsJson = JsonSerializer.Serialize(computedCharacter.BonusAbilitySelections, JsonOptions);
        SkillsJson = JsonSerializer.Serialize(computedCharacter.Skills, JsonOptions);
        SpellsJson = JsonSerializer.Serialize(computedCharacter.Spells, JsonOptions);
        InventoryJson = JsonSerializer.Serialize(computedCharacter.Inventory, JsonOptions);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public CharacterDto ToDto(bool canEdit = true)
    {
        var abilities = Deserialize<AbilityScoreDto>(AbilitiesJson);
        var skills = Deserialize<SkillLevelDto>(SkillsJson);
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
            HitPoints,
            Speed,
            ProficiencyBonus,
            PassivePerception,
            Notes,
            Deserialize<BaseAbilityScoreDto>(BaseAbilitiesJson),
            Deserialize<string>(BonusAbilitySelectionsJson),
            splitSkillSelections.Item1,
            splitSkillSelections.Item2,
            skills.Select(item => item.SkillId).ToList(),
            abilities,
            savingThrows,
            skills,
            Deserialize<string>(SpellsJson),
            Deserialize<string>(InventoryJson),
            CreatedAtUtc,
            UpdatedAtUtc);
    }

    public CharacterSummaryDto ToSummaryDto()
    {
        return new CharacterSummaryDto(
            Id,
            Name,
            Race,
            ClassName,
            Subclass,
            Level,
            ArmorClass,
            HitPoints,
            PassivePerception,
            Deserialize<SkillLevelDto>(SkillsJson));
    }

    private static List<T> Deserialize<T>(string source)
    {
        return JsonSerializer.Deserialize<List<T>>(source, JsonOptions) ?? [];
    }
}

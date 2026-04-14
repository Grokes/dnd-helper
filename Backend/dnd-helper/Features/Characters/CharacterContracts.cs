namespace dnd_helper.Features.Characters;

public sealed record CreateCharacterRequest(
    string Name,
    string RaceId,
    string ClassId,
    string BackgroundId,
    int Level,
    string Alignment,
    string Notes,
    List<BaseAbilityScoreDto> BaseAbilities,
    List<string> BonusAbilitySelections,
    List<string> RaceSkillSelections,
    List<string> ClassSkillSelections,
    List<string> Spells,
    List<string> Inventory);

public sealed record UpdateCharacterRequest(
    string Name,
    string RaceId,
    string ClassId,
    string BackgroundId,
    int Level,
    string Alignment,
    string Notes,
    List<BaseAbilityScoreDto> BaseAbilities,
    List<string> BonusAbilitySelections,
    List<string> RaceSkillSelections,
    List<string> ClassSkillSelections,
    List<string> Spells,
    List<string> Inventory);

public sealed record BaseAbilityScoreDto(string Key, int Score);

public sealed record SkillLevelDto(string SkillId, int Level);

public sealed record CharacterSummaryDto(
    Guid Id,
    string Name,
    string Race,
    string ClassName,
    string Subclass,
    int Level,
    int ArmorClass,
    int HitPoints,
    int PassivePerception,
    List<SkillLevelDto> Skills);

public sealed record CharacterDto(
    Guid Id,
    bool CanEdit,
    string RaceId,
    string ClassId,
    string BackgroundId,
    string Name,
    string Race,
    string ClassName,
    string Subclass,
    int Level,
    string Background,
    string Alignment,
    int ArmorClass,
    int HitPoints,
    int Speed,
    int ProficiencyBonus,
    int PassivePerception,
    string Notes,
    List<BaseAbilityScoreDto> BaseAbilities,
    List<string> BonusAbilitySelections,
    List<string> RaceSkillSelections,
    List<string> ClassSkillSelections,
    List<string> SkillProficiencies,
    List<AbilityScoreDto> Abilities,
    List<SavingThrowBonusDto> SavingThrows,
    List<SkillLevelDto> Skills,
    List<string> Spells,
    List<string> Inventory,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record AbilityScoreDto(string Key, int Score, int Modifier);

public sealed record SavingThrowBonusDto(string Ability, int Bonus, bool IsProficient);

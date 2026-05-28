using dnd_helper.Features.ReferenceData;

namespace dnd_helper.Features.Characters;

public static class CharacterBuilder
{
    private static readonly string[] AbilityOrder = ["STR", "DEX", "CON", "INT", "WIS", "CHA"];
    private static readonly HashSet<string> AllowedSkillIds =
    [
        "Acrobatics", "AnimalHandling", "Arcana", "Athletics", "Deception", "History",
        "Insight", "Intimidation", "Investigation", "Medicine", "Nature", "Perception",
        "Performance", "Persuasion", "Religion", "SleightOfHand", "Stealth", "Survival"
    ];

    private static readonly Dictionary<string, string> SkillAbilityMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Acrobatics"] = "DEX",
        ["AnimalHandling"] = "WIS",
        ["Arcana"] = "INT",
        ["Athletics"] = "STR",
        ["Deception"] = "CHA",
        ["History"] = "INT",
        ["Insight"] = "WIS",
        ["Intimidation"] = "CHA",
        ["Investigation"] = "INT",
        ["Medicine"] = "WIS",
        ["Nature"] = "INT",
        ["Perception"] = "WIS",
        ["Performance"] = "CHA",
        ["Persuasion"] = "CHA",
        ["Religion"] = "INT",
        ["SleightOfHand"] = "DEX",
        ["Stealth"] = "DEX",
        ["Survival"] = "WIS"
    };

    public static Dictionary<string, string[]>? Validate(
        CreateCharacterRequest request,
        RaceOptionDto? race,
        ClassOptionDto? characterClass,
        BackgroundOptionDto? background)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["У персонажа должно быть имя."];
        }

        if (race is null)
        {
            errors["raceId"] = ["Выбери расу из книги игрока."];
        }

        if (characterClass is null)
        {
            errors["classId"] = ["Выбери класс из книги игрока."];
        }

        if (background is null)
        {
            errors["backgroundId"] = ["Выбери предысторию из книги игрока."];
        }

        if (request.Level < 1 || request.Level > 20)
        {
            errors["level"] = ["Уровень персонажа должен быть в диапазоне от 1 до 20."];
        }

        if (!HasValidAbilityScores(request.BaseAbilities))
        {
            errors["baseAbilities"] = ["Каждая базовая характеристика должна быть задана вручную в пределах от 8 до 15."];
        }

        if (race?.BonusChoiceRule is { } choiceRule)
        {
            var validSelections = request.BonusAbilitySelections
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(choiceRule.AllowedAbilities.Contains)
                .ToList();

            if (validSelections.Count != choiceRule.Count)
            {
                errors["bonusAbilitySelections"] =
                [
                    $"Для расы {race.Name} нужно выбрать {choiceRule.Count} разные характеристики."
                ];
            }
        }
        else if (request.BonusAbilitySelections.Count > 0)
        {
            errors["bonusAbilitySelections"] = ["У выбранной расы нет дополнительных бонусов на выбор."];
        }

        var allSkillSelections = request.RaceSkillSelections.Concat(request.ClassSkillSelections).ToList();

        if (allSkillSelections.Any(skill => string.IsNullOrWhiteSpace(skill) || !AllowedSkillIds.Contains(skill)))
        {
            errors["raceSkillSelections"] = ["Навыки должны быть выбраны из доступного списка."];
        }

        if (allSkillSelections
            .GroupBy(skill => skill, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            errors["raceSkillSelections"] = ["Один и тот же навык нельзя выбрать одновременно из двух источников."];
        }

        if (errors.Count == 0 && race is not null && characterClass is not null && background is not null)
        {
            var raceSkillError = ValidateSkillSelections(request.RaceSkillSelections, race.SkillChoiceRule, "расы", GetFixedSkillProficiencies(race, background));
            if (raceSkillError is not null)
            {
                errors["raceSkillSelections"] = [raceSkillError];
            }

            var classSkillError = ValidateSkillSelections(request.ClassSkillSelections, characterClass.SkillChoiceRule, "класса", GetFixedSkillProficiencies(race, background).Concat(request.RaceSkillSelections).ToList());
            if (classSkillError is not null)
            {
                errors["classSkillSelections"] = [classSkillError];
            }
        }

        return errors.Count == 0 ? null : errors;
    }

    public static CharacterComputationResult Compute(
        string name,
        RaceOptionDto race,
        ClassOptionDto characterClass,
        BackgroundOptionDto background,
        int level,
        string alignment,
        string notes,
        IReadOnlyList<BaseAbilityScoreDto> baseAbilities,
        IReadOnlyList<string> bonusAbilitySelections,
        IReadOnlyList<string> raceSkillSelections,
        IReadOnlyList<string> classSkillSelections,
        IReadOnlyList<string> spells,
        IReadOnlyList<string> inventory)
    {
        var bonusMap = AbilityOrder.ToDictionary(ability => ability, _ => 0, StringComparer.OrdinalIgnoreCase);

        foreach (var bonus in race.Bonuses)
        {
            bonusMap[bonus.Ability] += bonus.Value;
        }

        if (race.BonusChoiceRule is not null)
        {
            foreach (var selection in bonusAbilitySelections.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                bonusMap[selection] += race.BonusChoiceRule.BonusValue;
            }
        }

        var baseMap = baseAbilities.ToDictionary(item => item.Key, item => item.Score, StringComparer.OrdinalIgnoreCase);

        var finalAbilities = AbilityOrder
            .Select(ability =>
            {
                var total = baseMap[ability] + bonusMap[ability];
                return new AbilityScoreDto(ability, total, CalculateModifier(total));
            })
            .ToList();

        var dexterityModifier = finalAbilities.First(item => item.Key == "DEX").Modifier;
        var constitutionModifier = finalAbilities.First(item => item.Key == "CON").Modifier;
        var wisdomModifier = finalAbilities.First(item => item.Key == "WIS").Modifier;

        var proficiencyBonus = CalculateProficiencyBonus(level);
        var passivePerception = 10 + wisdomModifier;
        var hitPoints = CalculateHitPoints(characterClass.HitDie, level, constitutionModifier);
        var armorClass = 10 + dexterityModifier;
        var finalSkillProficiencies = BuildFinalSkillProficiencies(raceSkillSelections, classSkillSelections, race, background);
        var skills = finalSkillProficiencies
            .Select(skillId => new SkillLevelDto(skillId, CalculateSkillBonus(skillId, finalAbilities, proficiencyBonus)))
            .OrderBy(item => item.SkillId)
            .ToList();

        return new CharacterComputationResult(
            race.Id,
            characterClass.Id,
            string.Empty,
            background.Id,
            name.Trim(),
            race.Name,
            characterClass.Name,
            background.Name,
            level,
            alignment.Trim(),
            notes.Trim(),
            baseAbilities.ToList(),
            bonusAbilitySelections.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            raceSkillSelections.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            classSkillSelections.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            armorClass,
            null,
            hitPoints,
            race.Speed,
            proficiencyBonus,
            passivePerception,
            finalAbilities,
            skills,
            [],
            spells.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct().ToList(),
            [],
            inventory.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct().ToList(),
            [],
            new Dictionary<string, object>(),
            []);
    }

    public static List<SavingThrowBonusDto> BuildSavingThrows(
        IReadOnlyList<AbilityScoreDto> abilities,
        IReadOnlyList<string> savingThrowProficiencies,
        int proficiencyBonus)
    {
        var proficiencySet = savingThrowProficiencies.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return AbilityOrder
            .Select(ability =>
            {
                var score = abilities.FirstOrDefault(item => item.Key == ability);
                var modifier = score?.Modifier ?? 0;
                var isProficient = proficiencySet.Contains(ability);
                return new SavingThrowBonusDto(
                    ability,
                    modifier + (isProficient ? proficiencyBonus : 0),
                    isProficient);
            })
            .ToList();
    }

    private static bool HasValidAbilityScores(IReadOnlyList<BaseAbilityScoreDto> abilities)
    {
        if (abilities.Count != AbilityOrder.Length)
        {
            return false;
        }

        var keys = abilities.Select(item => item.Key).OrderBy(item => item).ToArray();
        var expectedKeys = AbilityOrder.OrderBy(item => item).ToArray();

        return keys.SequenceEqual(expectedKeys)
            && abilities.All(item => item.Score >= 8 && item.Score <= 15);
    }

    public static (List<string> RaceSelections, List<string> ClassSelections) SplitSkillSelections(
        IReadOnlyList<string> skillProficiencies,
        RaceOptionDto race,
        ClassOptionDto characterClass,
        BackgroundOptionDto background)
    {
        var fixedSkills = GetFixedSkillProficiencies(race, background);
        var remaining = skillProficiencies
            .Except(fixedSkills, StringComparer.OrdinalIgnoreCase)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var raceSelections = new List<string>();
        var classSelections = new List<string>();

        if (race.SkillChoiceRule is not null)
        {
            raceSelections = remaining
                .Where(skill => race.SkillChoiceRule.AvailableSkills.Contains(skill))
                .Take(race.SkillChoiceRule.Count)
                .ToList();
            remaining = remaining.Except(raceSelections, StringComparer.OrdinalIgnoreCase).ToList();
        }

        classSelections = remaining
            .Where(skill => characterClass.SkillChoiceRule.AvailableSkills.Contains(skill))
            .Take(characterClass.SkillChoiceRule.Count)
            .ToList();

        return (raceSelections, classSelections);
    }

    private static string? ValidateSkillSelections(
        IReadOnlyList<string> selectedSkillProficiencies,
        SkillChoiceRuleDto? rule,
        string sourceName,
        IReadOnlyList<string> disallowedSkills)
    {
        if (rule is null)
        {
            return selectedSkillProficiencies.Count > 0
                ? $"У выбранной {sourceName} нет дополнительных навыков на выбор."
                : null;
        }

        var selections = selectedSkillProficiencies.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (selections.Any(skill => disallowedSkills.Contains(skill)))
        {
            return $"Нельзя повторно выбрать навык, уже полученный от других источников при выборе {sourceName}.";
        }

        if (selections.Count != rule.Count)
        {
            return $"Для выбранной {sourceName} нужно указать ровно {rule.Count} навыка.";
        }

        if (selections.Any(skill => !rule.AvailableSkills.Contains(skill)))
        {
            return $"Выбраны навыки, которые не соответствуют допустимым вариантам для выбранной {sourceName}.";
        }

        return null;
    }

    private static List<string> BuildFinalSkillProficiencies(
        IReadOnlyList<string> raceSkillSelections,
        IReadOnlyList<string> classSkillSelections,
        RaceOptionDto race,
        BackgroundOptionDto background)
    {
        return GetFixedSkillProficiencies(race, background)
            .Concat(raceSkillSelections)
            .Concat(classSkillSelections)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item)
            .ToList();
    }

    private static List<string> GetFixedSkillProficiencies(RaceOptionDto race, BackgroundOptionDto background)
    {
        return race.GrantedSkillProficiencies
            .Concat(background.GrantedSkillProficiencies)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int CalculateSkillBonus(string skillId, IReadOnlyList<AbilityScoreDto> abilities, int proficiencyBonus)
    {
        if (!SkillAbilityMap.TryGetValue(skillId, out var abilityKey))
        {
            return 0;
        }

        var abilityModifier = abilities.FirstOrDefault(item => item.Key == abilityKey)?.Modifier ?? 0;
        return abilityModifier + proficiencyBonus;
    }

    private static int CalculateModifier(int score) => (int)Math.Floor((score - 10) / 2.0);

    private static int CalculateProficiencyBonus(int level) => 2 + (level - 1) / 4;

    private static int CalculateHitPoints(int hitDie, int level, int constitutionModifier)
    {
        var firstLevel = hitDie + constitutionModifier;
        if (level == 1)
        {
            return firstLevel;
        }

        var averagePerLevel = (hitDie / 2) + 1 + constitutionModifier;
        return firstLevel + (level - 1) * averagePerLevel;
    }
}

public sealed record CharacterComputationResult(
    string RaceId,
    string ClassId,
    string SubclassId,
    string BackgroundId,
    string Name,
    string Race,
    string ClassName,
    string Background,
    int Level,
    string Alignment,
    string Notes,
    List<BaseAbilityScoreDto> BaseAbilities,
    List<string> BonusAbilitySelections,
    List<string> RaceSkillSelections,
    List<string> ClassSkillSelections,
    int ArmorClass,
    string? WeaponDamage,
    int HitPoints,
    int Speed,
    int ProficiencyBonus,
    int PassivePerception,
    List<AbilityScoreDto> Abilities,
    List<SkillLevelDto> Skills,
    List<SpellSlotDto> SpellSlots,
    List<string> KnownSpells,
    List<string> PreparedSpells,
    List<string> Inventory,
    List<string> ActiveEffects,
    Dictionary<string, object> ComputedSnapshot,
    List<CalculationTraceEntryDto> CalculationTrace);

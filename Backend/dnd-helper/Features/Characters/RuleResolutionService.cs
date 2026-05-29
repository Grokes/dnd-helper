using dnd_helper.Infrastructure.Persistence.Mongo;

namespace dnd_helper.Features.Characters;

public sealed class RuleResolutionService
{
    private static readonly string[] AbilityOrder = ["STR", "DEX", "CON", "INT", "WIS", "CHA"];
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

    public RuleResolutionResult Resolve(
        CreateCharacterRequest request,
        RaceDocument race,
        ClassDocument characterClass,
        BackgroundDocument background,
        IReadOnlyList<FeatureDocument> classFeatures,
        IReadOnlyList<EquipmentDocument> equipmentCatalog)
    {
        var trace = new List<CalculationTraceEntryDto>();
        var baseAbilityMap = request.BaseAbilities.ToDictionary(x => x.Key, x => x.Score, StringComparer.OrdinalIgnoreCase);
        var abilityBonuses = AbilityOrder.ToDictionary(x => x, _ => 0, StringComparer.OrdinalIgnoreCase);

        ApplyModifiers(race.Modifiers, abilityBonuses, trace, $"race:{race.Slug}");
        ApplyModifiers(background.Modifiers, abilityBonuses, trace, $"background:{background.Slug}");
        foreach (var feature in classFeatures)
        {
            ApplyModifiers(feature.Modifiers, abilityBonuses, trace, $"feature:{feature.Slug}");
        }

        var finalAbilities = AbilityOrder.Select(ability =>
        {
            var baseValue = baseAbilityMap.GetValueOrDefault(ability);
            var bonus = abilityBonuses.GetValueOrDefault(ability);
            var total = baseValue + bonus;
            trace.Add(new CalculationTraceEntryDto($"ability:{ability}", "base", "Базовое значение характеристики", baseValue, "set"));
            if (bonus != 0)
            {
                trace.Add(new CalculationTraceEntryDto($"ability:{ability}", "modifiers", "Сумма модификаторов правил", bonus, "add"));
            }

            return new AbilityScoreDto(ability, total, CalculateModifier(total));
        }).ToList();

        var level = request.Level;
        var proficiencyBonus = 2 + (level - 1) / 4;
        trace.Add(new CalculationTraceEntryDto("proficiency", "level", $"Бонус мастерства от уровня {level}", proficiencyBonus, "set"));

        var conModifier = finalAbilities.First(x => x.Key == "CON").Modifier;
        var dexModifier = finalAbilities.First(x => x.Key == "DEX").Modifier;
        var wisModifier = finalAbilities.First(x => x.Key == "WIS").Modifier;
        var hitPoints = CalculateHitPoints(characterClass.HitDie, level, conModifier);
        var armorClass = 10 + dexModifier;
        var passivePerception = 10 + wisModifier;
        var speed = race.Speed;
        string? weaponDamage = null;

        ApplyEquipmentEffects(request.Inventory, equipmentCatalog, dexModifier, ref armorClass, ref weaponDamage, trace);
        var spellSlots = BuildSpellSlots(characterClass.Slug, level);

        trace.Add(new CalculationTraceEntryDto("hitPoints", $"class:{characterClass.Slug}", "Расчет хитов по кости хитов и Телосложению", hitPoints, "set"));
        trace.Add(new CalculationTraceEntryDto("armorClass", "dexterity", "Итоговый КД с учетом экипировки", armorClass, "set"));
        trace.Add(new CalculationTraceEntryDto("passivePerception", "wisdom", "10 + модификатор Мудрости", passivePerception, "set"));

        var skillSet = BuildSkillSet(request, race, characterClass, background);
        var skills = skillSet
            .Select(skillId => new SkillLevelDto(skillId, CalculateSkillBonus(skillId, finalAbilities, proficiencyBonus)))
            .OrderBy(x => x.SkillId)
            .ToList();

        var snapshot = new Dictionary<string, object>
        {
            ["race"] = race.Slug,
            ["class"] = characterClass.Slug,
            ["background"] = background.Slug,
            ["level"] = level,
            ["abilityBonuses"] = abilityBonuses,
            ["classFeatures"] = classFeatures.Select(x => x.Slug).ToList(),
            ["savingThrows"] = characterClass.SavingThrowProficiencies,
            ["spellSlots"] = spellSlots
        };

        return new RuleResolutionResult(
            RaceName: race.Name,
            ClassName: characterClass.Name,
            BackgroundName: background.Name,
            Speed: speed,
            ArmorClass: armorClass,
            WeaponDamage: weaponDamage,
            HitPoints: hitPoints,
            ProficiencyBonus: proficiencyBonus,
            PassivePerception: passivePerception,
            Abilities: finalAbilities,
            Skills: skills,
            SkillProficiencies: skillSet.ToList(),
            SpellSlots: spellSlots,
            ComputedSnapshot: snapshot,
            CalculationTrace: trace);
    }

    public IReadOnlyList<FeatureDocument> ResolveClassFeaturesByLevel(ClassDocument characterClass, IReadOnlyList<FeatureDocument> allFeatures, int level)
    {
        var slugs = characterClass.Levels
            .Where(x => x.Level <= level)
            .SelectMany(x => x.FeatureSlugs)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return allFeatures.Where(x => slugs.Contains(x.Slug)).ToList();
    }

    public List<string> ValidateRequires(
        CreateCharacterRequest request,
        RaceDocument race,
        ClassDocument characterClass,
        BackgroundDocument background,
        IReadOnlyList<FeatureDocument> classFeatures)
    {
        var errors = new List<string>();
        var requiredEntries = race.Requires
            .Concat(characterClass.Requires)
            .Concat(background.Requires)
            .Concat(classFeatures.SelectMany(x => x.Requires));

        foreach (var requirement in requiredEntries)
        {
            if (requirement.Type.Equals("minLevel", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(requirement.Value, out var minLevel) &&
                request.Level < minLevel)
            {
                errors.Add($"Требование правил не выполнено: минимальный уровень {minLevel}.");
            }
        }

        return errors;
    }

    private static void ApplyModifiers(
        IEnumerable<ModifierEntry> modifiers,
        Dictionary<string, int> abilityBonuses,
        List<CalculationTraceEntryDto> trace,
        string source)
    {
        foreach (var modifier in modifiers.Where(x => x.Target.StartsWith("ability:", StringComparison.OrdinalIgnoreCase)))
        {
            var ability = modifier.Target["ability:".Length..].ToUpperInvariant();
            if (!abilityBonuses.ContainsKey(ability))
            {
                continue;
            }

            abilityBonuses[ability] += modifier.Value;
            trace.Add(new CalculationTraceEntryDto($"ability:{ability}", source, modifier.Reason, modifier.Value, modifier.Operation));
        }
    }

    private static int CalculateModifier(int score) => (int)Math.Floor((score - 10) / 2.0);

    private static int CalculateHitPoints(int hitDie, int level, int constitutionModifier)
    {
        var firstLevel = hitDie + constitutionModifier;
        if (level <= 1)
        {
            return firstLevel;
        }

        var avgPerLevel = hitDie / 2 + 1 + constitutionModifier;
        return firstLevel + (level - 1) * avgPerLevel;
    }

    private static HashSet<string> BuildSkillSet(
        CreateCharacterRequest request,
        RaceDocument race,
        ClassDocument characterClass,
        BackgroundDocument background)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddGrantSkills(race.Grants, set);
        AddGrantSkills(characterClass.Grants, set);
        AddGrantSkills(background.Grants, set);
        foreach (var skill in request.RaceSkillSelections.Concat(request.ClassSkillSelections))
        {
            set.Add(skill);
        }

        return set;
    }

    private static void AddGrantSkills(IEnumerable<GrantEntry> grants, HashSet<string> skillSet)
    {
        foreach (var grant in grants.Where(x => x.Type.Equals("skill", StringComparison.OrdinalIgnoreCase)))
        {
            skillSet.Add(grant.Value);
        }
    }

    private static int CalculateSkillBonus(string skillId, IReadOnlyList<AbilityScoreDto> abilities, int proficiencyBonus)
    {
        if (!SkillAbilityMap.TryGetValue(skillId, out var abilityKey))
        {
            return proficiencyBonus;
        }

        var abilityModifier = abilities.FirstOrDefault(x => x.Key == abilityKey)?.Modifier ?? 0;
        return abilityModifier + proficiencyBonus;
    }

    private static void ApplyEquipmentEffects(
        IReadOnlyList<string> inventory,
        IReadOnlyList<EquipmentDocument> equipmentCatalog,
        int dexModifier,
        ref int armorClass,
        ref string? weaponDamage,
        List<CalculationTraceEntryDto> trace)
    {
        var equipmentMap = equipmentCatalog.ToDictionary(item => item.Slug, item => item, StringComparer.OrdinalIgnoreCase);
        var equippedRaw = inventory.FirstOrDefault(item => item.StartsWith("equip:", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(equippedRaw))
        {
            return;
        }

        var equipped = ParseEquipToken(equippedRaw);
        if (equipped.TryGetValue("body", out var bodySlug) && equipmentMap.TryGetValue(bodySlug, out var bodyArmor) && bodyArmor.ArmorClassBase is not null)
        {
            var baseArmor = bodyArmor.ArmorClassBase.Value;
            armorClass = bodyArmor.Subcategory?.Equals("Light", StringComparison.OrdinalIgnoreCase) == true
                ? baseArmor + dexModifier
                : bodyArmor.Subcategory?.Equals("Medium", StringComparison.OrdinalIgnoreCase) == true
                    ? baseArmor + Math.Min(2, dexModifier)
                    : baseArmor;
            trace.Add(new CalculationTraceEntryDto("armorClass", $"equipment:{bodyArmor.Slug}", "КД от экипированного доспеха", armorClass, "set"));
        }

        var shieldSlug = string.Empty;
        if (equipped.TryGetValue("off", out var offSlug) &&
            equipmentMap.TryGetValue(offSlug, out var offHand) &&
            offHand.IsShield)
        {
            shieldSlug = offSlug;
        }
        else if (equipped.TryGetValue("main", out var mainShieldSlug) &&
                 equipmentMap.TryGetValue(mainShieldSlug, out var mainHand) &&
                 mainHand.IsShield)
        {
            shieldSlug = mainShieldSlug;
        }

        if (!string.IsNullOrWhiteSpace(shieldSlug) && equipmentMap.TryGetValue(shieldSlug, out var shieldItem))
        {
            var shieldBonus = shieldItem.ArmorClassBase ?? 2;
            armorClass += shieldBonus;
            trace.Add(new CalculationTraceEntryDto("armorClass", $"equipment:{shieldItem.Slug}", "Бонус КД от щита", shieldBonus, "add"));
        }

        if (equipped.TryGetValue("main", out var mainSlug) && equipmentMap.TryGetValue(mainSlug, out var weapon) && !string.IsNullOrWhiteSpace(weapon.DamageDice))
        {
            weaponDamage = $"{weapon.DamageDice} {weapon.DamageType}";
        }
    }

    private static Dictionary<string, string> ParseEquipToken(string raw)
    {
        var payload = raw["equip:".Length..];
        return payload
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Split('=', 2))
            .Where(parts => parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static List<SpellSlotDto> BuildSpellSlots(string classSlug, int level)
    {
        if (level < 1 || level > 20)
        {
            return [];
        }

        var fullCasters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bard", "cleric", "druid", "sorcerer", "wizard" };
        if (fullCasters.Contains(classSlug))
        {
            return FullCasterSlots[level - 1];
        }

        if (classSlug.Equals("paladin", StringComparison.OrdinalIgnoreCase) || classSlug.Equals("ranger", StringComparison.OrdinalIgnoreCase))
        {
            return HalfCasterSlots[level - 1];
        }

        if (classSlug.Equals("warlock", StringComparison.OrdinalIgnoreCase))
        {
            return WarlockSlots[level - 1];
        }

        return [];
    }

    private static readonly List<SpellSlotDto>[] FullCasterSlots =
    [
        [new(1, 2)], [new(1, 3)], [new(1, 4), new(2, 2)], [new(1, 4), new(2, 3)], [new(1, 4), new(2, 3), new(3, 2)],
        [new(1, 4), new(2, 3), new(3, 3)], [new(1, 4), new(2, 3), new(3, 3), new(4, 1)], [new(1, 4), new(2, 3), new(3, 3), new(4, 2)],
        [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 1)], [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 2)],
        [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 2), new(6, 1)], [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 2), new(6, 1)],
        [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 2), new(6, 1), new(7, 1)], [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 2), new(6, 1), new(7, 1)],
        [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 2), new(6, 1), new(7, 1), new(8, 1)], [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 2), new(6, 1), new(7, 1), new(8, 1)],
        [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 2), new(6, 1), new(7, 1), new(8, 1), new(9, 1)],
        [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 3), new(6, 1), new(7, 1), new(8, 1), new(9, 1)],
        [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 3), new(6, 2), new(7, 1), new(8, 1), new(9, 1)],
        [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 3), new(6, 2), new(7, 2), new(8, 1), new(9, 1)]
    ];

    private static readonly List<SpellSlotDto>[] HalfCasterSlots =
    [
        [], [], [new(1, 2)], [new(1, 3)], [new(1, 4), new(2, 2)], [new(1, 4), new(2, 2)], [new(1, 4), new(2, 3)],
        [new(1, 4), new(2, 3)], [new(1, 4), new(2, 3), new(3, 2)], [new(1, 4), new(2, 3), new(3, 2)],
        [new(1, 4), new(2, 3), new(3, 3)], [new(1, 4), new(2, 3), new(3, 3)], [new(1, 4), new(2, 3), new(3, 3), new(4, 1)],
        [new(1, 4), new(2, 3), new(3, 3), new(4, 1)], [new(1, 4), new(2, 3), new(3, 3), new(4, 2)], [new(1, 4), new(2, 3), new(3, 3), new(4, 2)],
        [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 1)], [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 1)],
        [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 2)], [new(1, 4), new(2, 3), new(3, 3), new(4, 3), new(5, 2)]
    ];

    private static readonly List<SpellSlotDto>[] WarlockSlots =
    [
        [new(1, 1)], [new(1, 2)], [new(2, 2)], [new(2, 2)], [new(3, 2)], [new(3, 2)], [new(4, 2)], [new(4, 2)],
        [new(5, 2)], [new(5, 2)], [new(5, 3)], [new(5, 3)], [new(5, 3)], [new(5, 3)], [new(5, 3)], [new(5, 3)],
        [new(5, 4)], [new(5, 4)], [new(5, 4)], [new(5, 4)]
    ];
}

public sealed record RuleResolutionResult(
    string RaceName,
    string ClassName,
    string BackgroundName,
    int Speed,
    int ArmorClass,
    string? WeaponDamage,
    int HitPoints,
    int ProficiencyBonus,
    int PassivePerception,
    List<AbilityScoreDto> Abilities,
    List<SkillLevelDto> Skills,
    List<string> SkillProficiencies,
    List<SpellSlotDto> SpellSlots,
    Dictionary<string, object> ComputedSnapshot,
    List<CalculationTraceEntryDto> CalculationTrace);

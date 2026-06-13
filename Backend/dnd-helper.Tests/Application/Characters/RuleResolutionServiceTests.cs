using dnd_helper.Infrastructure.Persistence.Mongo;
using Xunit;

namespace dnd_helper.Tests.Application.Characters;

public sealed class RuleResolutionServiceTests
{
    [Fact]
    public void Resolve_AppliesRaceModifiersEquipmentArmorShieldAndWeaponDamage()
    {
        var service = new RuleResolutionService();
        var request = CreateRequest(
            level: 3,
            inventory: ["equip:body=hide-armor;main=longsword;off=shield"],
            classSkillSelections: ["Athletics"]);

        var result = service.Resolve(
            request,
            CreateRace(),
            CreateClass("fighter"),
            CreateBackground(),
            [],
            CreateEquipmentCatalog());

        Assert.Equal(16, result.Abilities.Single(item => item.Key == "DEX").Score);
        Assert.Equal(3, result.Abilities.Single(item => item.Key == "DEX").Modifier);
        Assert.Equal(16, result.ArmorClass);
        Assert.Equal("1d8 slashing", result.WeaponDamage);
        Assert.Equal(28, result.HitPoints);
        Assert.Equal(2, result.ProficiencyBonus);
        Assert.Contains("Perception", result.SkillProficiencies);
        Assert.Contains("Athletics", result.SkillProficiencies);
        Assert.Contains(result.CalculationTrace, item => item.Target == "armorClass" && item.Source == "equipment:shield");
    }

    [Theory]
    [InlineData("wizard", 5, 3, 2)]
    [InlineData("paladin", 5, 2, 2)]
    [InlineData("warlock", 5, 3, 2)]
    [InlineData("fighter", 5, 0, 0)]
    public void Resolve_BuildsSpellSlotsByClassProgression(string classSlug, int level, int expectedHighestSlot, int expectedHighestSlotCount)
    {
        var service = new RuleResolutionService();

        var result = service.Resolve(
            CreateRequest(level: level),
            CreateRace(),
            CreateClass(classSlug),
            CreateBackground(),
            [],
            []);

        if (expectedHighestSlot == 0)
        {
            Assert.Empty(result.SpellSlots);
            return;
        }

        var highest = result.SpellSlots.MaxBy(item => item.SpellLevel);
        Assert.NotNull(highest);
        Assert.Equal(expectedHighestSlot, highest.SpellLevel);
        Assert.Equal(expectedHighestSlotCount, highest.Slots);
    }

    [Fact]
    public void ResolveClassFeaturesByLevel_ReturnsOnlyUnlockedClassFeatures()
    {
        var service = new RuleResolutionService();
        var characterClass = CreateClass("fighter");
        characterClass.Levels =
        [
            new LevelFeatureEntry(1, ["fighting-style"]),
            new LevelFeatureEntry(3, ["martial-archetype"]),
            new LevelFeatureEntry(5, ["extra-attack"])
        ];

        var result = service.ResolveClassFeaturesByLevel(
            characterClass,
            [
                new FeatureDocument { Slug = "fighting-style", Name = "Боевой стиль" },
                new FeatureDocument { Slug = "martial-archetype", Name = "Воинский архетип" },
                new FeatureDocument { Slug = "extra-attack", Name = "Дополнительная атака" }
            ],
            level: 3);

        Assert.Equal(["fighting-style", "martial-archetype"], result.Select(item => item.Slug).OrderBy(item => item));
    }

    [Fact]
    public void ValidateRequires_ReturnsMinLevelErrors()
    {
        var service = new RuleResolutionService();
        var classFeature = new FeatureDocument
        {
            Slug = "late-feature",
            Requires = [new RequirementEntry("minLevel", "5")]
        };

        var errors = service.ValidateRequires(
            CreateRequest(level: 3),
            CreateRace(),
            CreateClass("fighter"),
            CreateBackground(),
            [classFeature]);

        Assert.Single(errors);
        Assert.Contains("минимальный уровень 5", errors[0]);
    }

    private static CreateCharacterRequest CreateRequest(
        int level = 1,
        IReadOnlyList<string>? inventory = null,
        IReadOnlyList<string>? classSkillSelections = null)
    {
        return new CreateCharacterRequest(
            "Тестовый герой",
            "elf",
            "fighter",
            "soldier",
            level,
            "",
            "",
            [
                new BaseAbilityScoreDto("STR", 14),
                new BaseAbilityScoreDto("DEX", 14),
                new BaseAbilityScoreDto("CON", 14),
                new BaseAbilityScoreDto("INT", 10),
                new BaseAbilityScoreDto("WIS", 12),
                new BaseAbilityScoreDto("CHA", 10)
            ],
            [],
            [],
            (classSkillSelections ?? []).ToList(),
            [],
            (inventory ?? []).ToList());
    }

    private static RaceDocument CreateRace()
    {
        return new RaceDocument
        {
            Slug = "elf",
            Name = "Эльф",
            Speed = 30,
            Grants = [new GrantEntry("skill", "Perception")],
            Modifiers = [new ModifierEntry("ability:DEX", 2, "add", "Ловкость эльфа")]
        };
    }

    private static ClassDocument CreateClass(string slug)
    {
        return new ClassDocument
        {
            Slug = slug,
            Name = slug,
            HitDie = slug == "wizard" ? 6 : 10,
            SavingThrowProficiencies = ["STR", "CON"]
        };
    }

    private static BackgroundDocument CreateBackground()
    {
        return new BackgroundDocument
        {
            Slug = "soldier",
            Name = "Солдат"
        };
    }

    private static IReadOnlyList<EquipmentDocument> CreateEquipmentCatalog()
    {
        return
        [
            new EquipmentDocument
            {
                Slug = "hide-armor",
                Name = "Шкурный доспех",
                Category = "Armor",
                Subcategory = "Medium",
                ArmorClassBase = 12,
                EquipSlot = "body"
            },
            new EquipmentDocument
            {
                Slug = "shield",
                Name = "Щит",
                Category = "Armor",
                Subcategory = "Shield",
                ArmorClassBase = 2,
                IsShield = true,
                EquipSlot = "off-hand"
            },
            new EquipmentDocument
            {
                Slug = "longsword",
                Name = "Длинный меч",
                Category = "Martial Weapon",
                DamageDice = "1d8",
                DamageType = "slashing",
                EquipSlot = "hand"
            }
        ];
    }
}

using dnd_helper.Application.ReferenceData;
using Xunit;

namespace dnd_helper.Tests.Application.Characters;

public sealed class CharacterBuilderTests
{
    [Fact]
    public void Validate_RejectsDuplicateSkillSelectionsAcrossSources()
    {
        var race = CreateRace(skillChoiceRule: new SkillChoiceRuleDto(1, ["Insight", "Perception"], "Выбери навык расы."));
        var characterClass = CreateClass(new SkillChoiceRuleDto(1, ["Insight", "Perception"], "Выбери навык класса."));
        var background = CreateBackground();
        var request = CreateRequest(
            raceSkillSelections: ["Insight"],
            classSkillSelections: ["Insight"]);

        var errors = CharacterBuilder.Validate(request, race, characterClass, background);

        Assert.NotNull(errors);
        Assert.Contains("raceSkillSelections", errors.Keys);
        Assert.Contains("Один и тот же навык", errors["raceSkillSelections"][0]);
    }

    [Fact]
    public void Compute_AppliesAbilityBonusesAndBuildsDerivedStats()
    {
        var race = CreateRace(
            bonuses: [new AbilityBonusDto("DEX", 2)],
            grantedSkillProficiencies: ["Stealth"]);
        var characterClass = CreateClass(new SkillChoiceRuleDto(1, ["Perception", "Athletics"], "Выбери навык класса."));
        var background = CreateBackground(grantedSkillProficiencies: ["Athletics"]);

        var result = CharacterBuilder.Compute(
            "  Лаэран  ",
            race,
            characterClass,
            background,
            level: 5,
            alignment: "  нейтральный  ",
            notes: "  заметка  ",
            baseAbilities: CreateBaseAbilities(dexterity: 14, constitution: 14, wisdom: 12),
            bonusAbilitySelections: [],
            raceSkillSelections: [],
            classSkillSelections: ["Perception"],
            spells: [" mage-hand ", "mage-hand"],
            inventory: ["item:rapier", "item:rapier"]);

        Assert.Equal("Лаэран", result.Name);
        Assert.Equal("нейтральный", result.Alignment);
        Assert.Equal("заметка", result.Notes);
        Assert.Equal(16, result.Abilities.Single(item => item.Key == "DEX").Score);
        Assert.Equal(3, result.Abilities.Single(item => item.Key == "DEX").Modifier);
        Assert.Equal(13, result.ArmorClass);
        Assert.Equal(44, result.HitPoints);
        Assert.Equal(3, result.ProficiencyBonus);
        Assert.Equal(11, result.PassivePerception);
        Assert.Equal(4, result.Skills.Single(item => item.SkillId == "Perception").Level);
        Assert.Equal(3, result.Skills.Single(item => item.SkillId == "Athletics").Level);
        Assert.Equal(["mage-hand"], result.KnownSpells);
        Assert.Equal(["item:rapier"], result.Inventory);
    }

    [Fact]
    public void Validate_RejectsLevelAndAbilityScoresOutsideAllowedRanges()
    {
        var request = CreateRequest() with
        {
            Level = 21,
            BaseAbilities =
            [
                new BaseAbilityScoreDto("STR", 10),
                new BaseAbilityScoreDto("DEX", 10),
                new BaseAbilityScoreDto("CON", 10),
                new BaseAbilityScoreDto("INT", 10),
                new BaseAbilityScoreDto("WIS", 10),
                new BaseAbilityScoreDto("CHA", 19)
            ]
        };

        var errors = CharacterBuilder.Validate(request, CreateRace(), CreateClass(new SkillChoiceRuleDto(0, [], "")), CreateBackground());

        Assert.NotNull(errors);
        Assert.Contains("level", errors.Keys);
        Assert.Contains("baseAbilities", errors.Keys);
    }

    [Fact]
    public void BuildSavingThrows_AddsProficiencyOnlyForClassSavingThrows()
    {
        var savingThrows = CharacterBuilder.BuildSavingThrows(
            [
                new AbilityScoreDto("STR", 16, 3),
                new AbilityScoreDto("DEX", 12, 1),
                new AbilityScoreDto("CON", 14, 2),
                new AbilityScoreDto("INT", 10, 0),
                new AbilityScoreDto("WIS", 8, -1),
                new AbilityScoreDto("CHA", 10, 0)
            ],
            ["STR", "CON"],
            proficiencyBonus: 3);

        Assert.Equal(6, savingThrows.Single(item => item.Ability == "STR").Bonus);
        Assert.Equal(5, savingThrows.Single(item => item.Ability == "CON").Bonus);
        Assert.Equal(1, savingThrows.Single(item => item.Ability == "DEX").Bonus);
        Assert.False(savingThrows.Single(item => item.Ability == "DEX").IsProficient);
    }

    private static CreateCharacterRequest CreateRequest(
        IReadOnlyList<string>? raceSkillSelections = null,
        IReadOnlyList<string>? classSkillSelections = null)
    {
        return new CreateCharacterRequest(
            "Тестовый персонаж",
            "test-race",
            "test-class",
            "test-background",
            1,
            "",
            "",
            CreateBaseAbilities().ToList(),
            [],
            (raceSkillSelections ?? []).ToList(),
            (classSkillSelections ?? []).ToList(),
            [],
            []);
    }

    private static RaceOptionDto CreateRace(
        IReadOnlyList<AbilityBonusDto>? bonuses = null,
        SkillChoiceRuleDto? skillChoiceRule = null,
        IReadOnlyList<string>? grantedSkillProficiencies = null)
    {
        return new RaceOptionDto(
            "test-race",
            "Тестовая раса",
            "Тестовая раса",
            30,
            bonuses ?? [],
            null,
            grantedSkillProficiencies ?? [],
            skillChoiceRule,
            [],
            "Тестовая раса");
    }

    private static ClassOptionDto CreateClass(SkillChoiceRuleDto skillChoiceRule)
    {
        return new ClassOptionDto(
            "test-class",
            "Тестовый класс",
            10,
            ["STR"],
            ["STR", "CON"],
            skillChoiceRule,
            [],
            "Тестовый класс");
    }

    private static BackgroundOptionDto CreateBackground(IReadOnlyList<string>? grantedSkillProficiencies = null)
    {
        return new BackgroundOptionDto(
            "test-background",
            "Тестовая предыстория",
            grantedSkillProficiencies ?? [],
            [],
            "Тестовая предыстория");
    }

    private static IReadOnlyList<BaseAbilityScoreDto> CreateBaseAbilities(
        int strength = 10,
        int dexterity = 10,
        int constitution = 10,
        int intelligence = 10,
        int wisdom = 10,
        int charisma = 10)
    {
        return
        [
            new BaseAbilityScoreDto("STR", strength),
            new BaseAbilityScoreDto("DEX", dexterity),
            new BaseAbilityScoreDto("CON", constitution),
            new BaseAbilityScoreDto("INT", intelligence),
            new BaseAbilityScoreDto("WIS", wisdom),
            new BaseAbilityScoreDto("CHA", charisma)
        ];
    }
}

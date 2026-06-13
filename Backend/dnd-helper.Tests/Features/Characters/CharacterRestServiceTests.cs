using System.Text.Json;
using Xunit;

namespace dnd_helper.Tests.Features.Characters;

public sealed class CharacterRestServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ApplyRest_ShortRestSpendsHitDiceAndHealsCharacter()
    {
        var character = CreateCharacter();
        var service = new CharacterRestService(CreateRoller(4, 4));

        var outcome = service.ApplyRest(character, new CharacterRestRequest("short", 2), canEdit: true);

        Assert.True(outcome.IsSuccess);
        Assert.NotNull(outcome.Result);
        Assert.Equal(12, character.CurrentHitPoints);
        Assert.Equal(3, character.SpentHitDice);
        Assert.Equal(0, outcome.Result.AvailableHitDice);
        Assert.Contains("Короткий отдых", outcome.Result.Details);
    }

    [Fact]
    public void ApplyRest_LongRestRestoresHitPointsHitDiceAndSpellSlots()
    {
        var character = CreateCharacter();
        character.SpentSpellSlotsJson = JsonSerializer.Serialize(new Dictionary<int, int> { [1] = 1 }, JsonOptions);

        var service = new CharacterRestService(CreateRoller());

        var outcome = service.ApplyRest(character, new CharacterRestRequest("long", null), canEdit: true);

        Assert.True(outcome.IsSuccess);
        Assert.NotNull(outcome.Result);
        Assert.Equal(12, character.CurrentHitPoints);
        Assert.Equal(0, character.SpentHitDice);
        Assert.Equal(2, outcome.Result.SpellSlots.Single(slot => slot.SpellLevel == 1).Slots);
        Assert.Equal("{}", character.SpentSpellSlotsJson);
    }

    [Fact]
    public void ApplyRest_RejectsUnknownRestType()
    {
        var service = new CharacterRestService(CreateRoller());

        var outcome = service.ApplyRest(CreateCharacter(), new CharacterRestRequest("campfire", null), canEdit: true);

        Assert.False(outcome.IsSuccess);
        Assert.NotNull(outcome.Errors);
        Assert.Contains("restType", outcome.Errors.Keys);
    }

    private static CharacterEntity CreateCharacter()
    {
        return new CharacterEntity
        {
            Id = Guid.NewGuid(),
            OwnerUserId = "user-1",
            Name = "Тестовый герой",
            RaceId = "human",
            ClassId = "fighter",
            BackgroundId = "soldier",
            Level = 3,
            HitPoints = 12,
            MaxHitPoints = 12,
            CurrentHitPoints = 4,
            SpentHitDice = 1,
            ProficiencyBonus = 2,
            AbilitiesJson = JsonSerializer.Serialize(
                new List<AbilityScoreDto>
                {
                    new("STR", 10, 0),
                    new("DEX", 10, 0),
                    new("CON", 14, 2),
                    new("INT", 10, 0),
                    new("WIS", 10, 0),
                    new("CHA", 10, 0)
                },
                JsonOptions),
            BaseAbilitiesJson = "[]",
            BonusAbilitySelectionsJson = "[]",
            SkillsJson = "[]",
            SpellSlotsJson = JsonSerializer.Serialize(new List<SpellSlotDto> { new(1, 2) }, JsonOptions),
            SpentSpellSlotsJson = "{}",
            KnownSpellsJson = "[]",
            InventoryJson = "[]",
            CalculationTraceJson = "[]"
        };
    }

    private static DiceRoller CreateRoller(params int[] rolls)
    {
        var index = 0;
        return new DiceRoller((minimumInclusive, maximumExclusive) =>
        {
            if (rolls.Length == 0)
            {
                return minimumInclusive;
            }

            var value = rolls[Math.Min(index, rolls.Length - 1)];
            index++;
            return Math.Clamp(value, minimumInclusive, maximumExclusive - 1);
        });
    }
}

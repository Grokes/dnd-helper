using System.Text.Json;
using dnd_helper.Application.Rules;
using dnd_helper.Infrastructure.Persistence.Mongo;
using dnd_helper.Infrastructure.Seeding;
using Xunit;

namespace dnd_helper.Tests.Application.Characters;

public sealed class CharacterSpellServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CastAsync_KnownLeveledSpellSpendsSlotAndRollsDamage()
    {
        var character = CreateCharacter(
            knownSpells: ["burning-hands"],
            slots: [new SpellSlotDto(1, 2)]);
        var service = CreateService(
            [CreateSpell("burning-hands", "Огненные ладони", 1)],
            rolls: [2, 3, 4]);

        var outcome = await service.CastAsync(character, new CharacterCastSpellRequest("burning-hands", null), CancellationToken.None);

        Assert.True(outcome.IsSuccess);
        Assert.NotNull(outcome.Result);
        Assert.True(outcome.Result.ConsumedSlot);
        Assert.Equal(1, outcome.Result.SlotLevel);
        Assert.Equal("3d6", outcome.Result.DamageDice);
        Assert.Equal(9, outcome.Result.DamageTotal);
        Assert.Equal(1, outcome.Result.SpellSlots.Single(slot => slot.SpellLevel == 1).Slots);

        var spentSlots = JsonSerializer.Deserialize<Dictionary<int, int>>(character.SpentSpellSlotsJson, JsonOptions);
        Assert.Equal(1, spentSlots?[1]);
    }

    [Fact]
    public async Task CastAsync_CantripDoesNotSpendSpellSlot()
    {
        var character = CreateCharacter(
            knownSpells: ["fire-bolt"],
            slots: [new SpellSlotDto(1, 1)]);
        var service = CreateService(
            [CreateSpell("fire-bolt", "Огненный снаряд", 0)],
            rolls: [7]);

        var outcome = await service.CastAsync(character, new CharacterCastSpellRequest("fire-bolt", null), CancellationToken.None);

        Assert.True(outcome.IsSuccess);
        Assert.NotNull(outcome.Result);
        Assert.False(outcome.Result.ConsumedSlot);
        Assert.Null(outcome.Result.SlotLevel);
        Assert.Equal("1d10", outcome.Result.DamageDice);
        Assert.Equal(7, outcome.Result.DamageTotal);
        Assert.Equal("{}", character.SpentSpellSlotsJson);
    }

    [Fact]
    public async Task CastAsync_UsesNormalizedSpellRowsWhenTheyAreLoaded()
    {
        var character = CreateCharacter(
            knownSpells: [],
            slots: []);
        character.KnownSpells.Add(new CharacterKnownSpellEntity
        {
            CharacterId = character.Id,
            Character = character,
            SpellSlug = "burning-hands"
        });
        character.SpellSlots.Add(new CharacterSpellSlotEntity
        {
            CharacterId = character.Id,
            Character = character,
            SpellLevel = 1,
            MaxSlots = 2,
            SpentSlots = 0
        });
        var service = CreateService(
            [CreateSpell("burning-hands", "Огненные ладони", 1)],
            rolls: [1, 1, 1]);

        var outcome = await service.CastAsync(character, new CharacterCastSpellRequest("burning-hands", null), CancellationToken.None);

        Assert.True(outcome.IsSuccess);
        Assert.Equal(1, character.SpellSlots.Single().SpentSlots);
        Assert.Equal(1, outcome.Result!.SpellSlots.Single(slot => slot.SpellLevel == 1).Slots);

        var spentSlots = JsonSerializer.Deserialize<Dictionary<int, int>>(character.SpentSpellSlotsJson, JsonOptions);
        Assert.Equal(1, spentSlots?[1]);
    }

    [Fact]
    public async Task CastAsync_RejectsSpellThatCharacterDoesNotKnow()
    {
        var character = CreateCharacter(
            knownSpells: ["magic-missile"],
            slots: [new SpellSlotDto(1, 1)]);
        var service = CreateService([CreateSpell("burning-hands", "Огненные ладони", 1)]);

        var outcome = await service.CastAsync(character, new CharacterCastSpellRequest("burning-hands", null), CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.NotNull(outcome.Errors);
        Assert.Contains("spellSlug", outcome.Errors.Keys);
    }

    [Fact]
    public async Task CastAsync_RejectsLeveledSpellWhenSlotIsAlreadySpent()
    {
        var character = CreateCharacter(
            knownSpells: ["burning-hands"],
            slots: [new SpellSlotDto(1, 1)],
            spentSlots: new Dictionary<int, int> { [1] = 1 });
        var service = CreateService([CreateSpell("burning-hands", "Огненные ладони", 1)]);

        var outcome = await service.CastAsync(character, new CharacterCastSpellRequest("burning-hands", 1), CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.NotNull(outcome.Errors);
        Assert.Contains("slotLevel", outcome.Errors.Keys);
    }

    private static CharacterSpellService CreateService(IReadOnlyList<SpellDocument> spells, params int[] rolls)
    {
        var index = 0;
        var diceRoller = new DiceRoller((minimumInclusive, maximumExclusive) =>
        {
            if (rolls.Length == 0)
            {
                return minimumInclusive;
            }

            var roll = rolls[Math.Min(index, rolls.Length - 1)];
            index++;
            return Math.Clamp(roll, minimumInclusive, maximumExclusive - 1);
        });

        return new CharacterSpellService(new FakeRulesCatalogRepository(spells), diceRoller);
    }

    private static SpellDocument CreateSpell(string slug, string name, int spellLevel)
    {
        return new SpellDocument
        {
            RulesetId = RulesDatabaseSeeder.RulesetId,
            Slug = slug,
            Name = name,
            SpellLevel = spellLevel,
            Effects = [new EffectEntry("summary", "set", 0, "Тестовое описание заклинания.")]
        };
    }

    private static CharacterEntity CreateCharacter(
        IReadOnlyList<string> knownSpells,
        IReadOnlyList<SpellSlotDto> slots,
        Dictionary<int, int>? spentSlots = null)
    {
        return new CharacterEntity
        {
            Id = Guid.NewGuid(),
            OwnerUserId = "user-1",
            Name = "Тестовый маг",
            RaceId = "human",
            ClassId = "wizard",
            BackgroundId = "sage",
            Level = 1,
            HitPoints = 8,
            MaxHitPoints = 8,
            CurrentHitPoints = 8,
            ProficiencyBonus = 2,
            KnownSpellsJson = JsonSerializer.Serialize(knownSpells, JsonOptions),
            SpellSlotsJson = JsonSerializer.Serialize(slots, JsonOptions),
            SpentSpellSlotsJson = JsonSerializer.Serialize(spentSlots ?? [], JsonOptions),
            AbilitiesJson = "[]",
            BaseAbilitiesJson = "[]",
            BonusAbilitySelectionsJson = "[]",
            SkillsJson = "[]",
            InventoryJson = "[]",
            CalculationTraceJson = "[]"
        };
    }

    private sealed class FakeRulesCatalogRepository : IRulesCatalogRepository
    {
        private readonly IReadOnlyList<SpellDocument> spells;

        public FakeRulesCatalogRepository(IReadOnlyList<SpellDocument> spells)
        {
            this.spells = spells;
        }

        public Task EnsureIndexesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<RulesetDocument>> GetRulesetsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RulesetDocument>>([]);
        public Task<IReadOnlyList<RaceDocument>> GetRacesAsync(string rulesetId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RaceDocument>>([]);
        public Task<IReadOnlyList<ClassDocument>> GetClassesAsync(string rulesetId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ClassDocument>>([]);
        public Task<IReadOnlyList<BackgroundDocument>> GetBackgroundsAsync(string rulesetId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BackgroundDocument>>([]);
        public Task<IReadOnlyList<FeatureDocument>> GetFeaturesAsync(string rulesetId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FeatureDocument>>([]);
        public Task<IReadOnlyList<SpellDocument>> GetSpellsAsync(string rulesetId, CancellationToken cancellationToken = default) => Task.FromResult(spells);
        public Task<IReadOnlyList<EquipmentDocument>> GetEquipmentAsync(string rulesetId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<EquipmentDocument>>([]);
        public Task<IReadOnlyList<MonsterDocument>> GetMonstersAsync(string rulesetId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<MonsterDocument>>([]);
        public Task<IReadOnlyList<ConditionDocument>> GetConditionsAsync(string rulesetId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ConditionDocument>>([]);
        public Task<RaceDocument?> GetRaceBySlugAsync(string rulesetId, string slug, CancellationToken cancellationToken = default) => Task.FromResult<RaceDocument?>(null);
        public Task<ClassDocument?> GetClassBySlugAsync(string rulesetId, string slug, CancellationToken cancellationToken = default) => Task.FromResult<ClassDocument?>(null);
        public Task<BackgroundDocument?> GetBackgroundBySlugAsync(string rulesetId, string slug, CancellationToken cancellationToken = default) => Task.FromResult<BackgroundDocument?>(null);
        public Task<IReadOnlyList<FeatureDocument>> GetFeaturesBySlugsAsync(string rulesetId, IEnumerable<string> slugs, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FeatureDocument>>([]);
        public Task UpsertRulesetAsync(RulesetDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpsertRaceAsync(RaceDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpsertClassAsync(ClassDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpsertBackgroundAsync(BackgroundDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpsertFeatureAsync(FeatureDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpsertSpellAsync(SpellDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpsertEquipmentAsync(EquipmentDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpsertMonsterAsync(MonsterDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpsertConditionAsync(ConditionDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

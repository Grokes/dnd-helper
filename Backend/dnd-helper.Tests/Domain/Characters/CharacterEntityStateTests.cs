using Xunit;

namespace dnd_helper.Tests.Domain.Characters;

public sealed class CharacterEntityStateTests
{
    [Fact]
    public void ToDto_PrefersNormalizedCharacterStateRows()
    {
        var character = new CharacterEntity
        {
            Id = Guid.NewGuid(),
            OwnerUserId = "user-1",
            RaceId = "human",
            ClassId = "wizard",
            BackgroundId = "sage",
            Name = "Мира",
            Race = "Человек",
            ClassName = "Волшебник",
            Background = "Мудрец",
            Level = 1,
            HitPoints = 8,
            MaxHitPoints = 8,
            CurrentHitPoints = 6,
            Speed = 30,
            ProficiencyBonus = 2,
            PassivePerception = 12,
            AbilitiesJson = "[]",
            BaseAbilitiesJson = "[]",
            BonusAbilitySelectionsJson = "[]",
            SkillsJson = "[]",
            KnownSpellsJson = "[]",
            SpellSlotsJson = "[]",
            SpentSpellSlotsJson = "{}",
            InventoryJson = "[]",
            CalculationTraceJson = "[]"
        };

        character.BaseAbilities.Add(new CharacterBaseAbilityEntity { CharacterId = character.Id, Ability = "INT", Score = 15 });
        character.Abilities.Add(new CharacterAbilityEntity { CharacterId = character.Id, Ability = "INT", Score = 16, Modifier = 3 });
        character.SelectedOptions.Add(new CharacterSelectedOptionEntity
        {
            Id = Guid.NewGuid(),
            CharacterId = character.Id,
            Source = "class",
            OptionType = "skill",
            Value = "Arcana"
        });
        character.SkillProficiencies.Add(new CharacterSkillProficiencyEntity { CharacterId = character.Id, SkillId = "Arcana", Bonus = 5 });
        character.SavingThrowProficiencies.Add(new CharacterSavingThrowProficiencyEntity { CharacterId = character.Id, Ability = "INT" });
        character.KnownSpells.Add(new CharacterKnownSpellEntity { CharacterId = character.Id, SpellSlug = "magic-missile" });
        character.SpellSlots.Add(new CharacterSpellSlotEntity { CharacterId = character.Id, SpellLevel = 1, MaxSlots = 2, SpentSlots = 1 });
        character.InventoryItems.Add(new CharacterInventoryItemEntity
        {
            Id = Guid.NewGuid(),
            CharacterId = character.Id,
            ItemRef = "item:dagger",
            Quantity = 2,
            CreatedAtUtc = DateTime.UtcNow
        });
        character.CalculationTraceEntries.Add(new CharacterCalculationTraceEntryEntity
        {
            Id = Guid.NewGuid(),
            CharacterId = character.Id,
            Order = 0,
            Target = "armorClass",
            Source = "base",
            Reason = "Базовый КД",
            Value = 10,
            Operation = "set"
        });

        var dto = character.ToDto();

        Assert.Equal(15, dto.BaseAbilities.Single().Score);
        Assert.Equal(16, dto.Abilities.Single().Score);
        Assert.Equal(["Arcana"], dto.ClassSkillSelections);
        Assert.Equal(5, dto.Skills.Single().Level);
        Assert.True(dto.SavingThrows.Single(item => item.Ability == "INT").IsProficient);
        Assert.Equal(["magic-missile"], dto.KnownSpells);
        Assert.Equal(1, dto.SpellSlots.Single(slot => slot.SpellLevel == 1).Slots);
        Assert.Equal(2, dto.MaxSpellSlots.Single(slot => slot.SpellLevel == 1).Slots);
        Assert.Equal(["item:dagger", "item:dagger"], dto.Inventory);
        Assert.Equal("armorClass", dto.CalculationTrace.Single().Target);
    }

    [Fact]
    public void UpdateFromComputed_StoresEquippedInventoryAsStructuredRows()
    {
        var character = new CharacterEntity
        {
            Id = Guid.NewGuid(),
            OwnerUserId = "user-1"
        };
        var computed = new CharacterComputationResult(
            RaceId: "human",
            ClassId: "fighter",
            SubclassId: string.Empty,
            BackgroundId: "soldier",
            Name: "Рин",
            Race: "Человек",
            ClassName: "Воин",
            Background: "Солдат",
            Level: 1,
            Alignment: string.Empty,
            Notes: string.Empty,
            BaseAbilities: [],
            BonusAbilitySelections: [],
            RaceSkillSelections: [],
            ClassSkillSelections: [],
            ArmorClass: 16,
            WeaponDamage: "1d8 рубящий",
            HitDie: 10,
            HitPoints: 12,
            Speed: 30,
            ProficiencyBonus: 2,
            PassivePerception: 10,
            Abilities: [],
            Skills: [],
            SpellSlots: [],
            SavingThrowProficiencies: ["STR", "CON"],
            KnownSpells: [],
            Inventory: ["item:rope", "item:rope", "equip:body=chain-mail;main=longsword;off=shield"],
            ComputedSnapshot: [],
            CalculationTrace: []);

        character.UpdateFromComputed(computed);
        var dto = character.ToDto();

        Assert.Contains(character.InventoryItems, item => item.ItemRef == "item:rope" && item.Quantity == 2 && !item.IsEquipped);
        Assert.Contains(character.InventoryItems, item => item.ItemRef == "chain-mail" && item.EquipmentSlot == "body" && item.IsEquipped);
        Assert.Contains(character.InventoryItems, item => item.ItemRef == "longsword" && item.EquipmentSlot == "main" && item.IsEquipped);
        Assert.Contains(character.InventoryItems, item => item.ItemRef == "shield" && item.EquipmentSlot == "off" && item.IsEquipped);
        Assert.Equal(["item:rope", "item:rope", "equip:body=chain-mail;main=longsword;off=shield"], dto.Inventory);
    }

    [Fact]
    public void ReplaceNormalizedStateFrom_ReplacesEditableCollections()
    {
        var character = CreateEditableCharacter();
        character.SelectedOptions.Add(new CharacterSelectedOptionEntity
        {
            Id = Guid.NewGuid(),
            CharacterId = character.Id,
            Source = "class",
            OptionType = "skill",
            Value = "Arcana"
        });
        character.KnownSpells.Add(new CharacterKnownSpellEntity { CharacterId = character.Id, SpellSlug = "shield" });
        character.InventoryItems.Add(new CharacterInventoryItemEntity
        {
            Id = Guid.NewGuid(),
            CharacterId = character.Id,
            ItemRef = "item:rope",
            Quantity = 1,
            CreatedAtUtc = DateTime.UtcNow
        });

        var rebuilt = CreateEditableCharacter();
        rebuilt.Notes = "Новые заметки";
        rebuilt.BonusAbilitySelectionsJson = """
            {"bonusAbilitySelections":[],"raceSkillSelections":[],"classSkillSelections":["Stealth"]}
            """;
        rebuilt.KnownSpellsJson = """["magic-missile"]""";
        rebuilt.InventoryJson = """["item:dagger","equip:main=dagger;off=shield"]""";
        rebuilt.ReplaceNormalizedState(new CharacterComputationResult(
            RaceId: rebuilt.RaceId,
            ClassId: rebuilt.ClassId,
            SubclassId: rebuilt.Subclass,
            BackgroundId: rebuilt.BackgroundId,
            Name: rebuilt.Name,
            Race: rebuilt.Race,
            ClassName: rebuilt.ClassName,
            Background: rebuilt.Background,
            Level: rebuilt.Level,
            Alignment: rebuilt.Alignment,
            Notes: rebuilt.Notes,
            BaseAbilities: [new BaseAbilityScoreDto("DEX", 14)],
            BonusAbilitySelections: [],
            RaceSkillSelections: [],
            ClassSkillSelections: ["Stealth"],
            ArmorClass: 12,
            WeaponDamage: "1d4 колющий",
            HitDie: 8,
            HitPoints: 8,
            Speed: 30,
            ProficiencyBonus: 2,
            PassivePerception: 10,
            Abilities: [new AbilityScoreDto("DEX", 14, 2)],
            Skills: [new SkillLevelDto("Stealth", 4)],
            SpellSlots: [new SpellSlotDto(1, 2)],
            SavingThrowProficiencies: ["DEX"],
            KnownSpells: ["magic-missile"],
            Inventory: ["item:dagger", "equip:main=dagger;off=shield"],
            ComputedSnapshot: [],
            CalculationTrace: []));

        character.Notes = rebuilt.Notes;
        character.ReplaceNormalizedStateFrom(rebuilt);
        var dto = character.ToDto();

        Assert.Equal("Новые заметки", dto.Notes);
        Assert.Equal(["Stealth"], dto.ClassSkillSelections);
        Assert.Equal(["magic-missile"], dto.KnownSpells);
        Assert.Equal(["item:dagger", "equip:main=dagger;off=shield"], dto.Inventory);
        Assert.DoesNotContain(dto.KnownSpells, spell => spell == "shield");
        Assert.DoesNotContain(dto.Inventory, item => item == "item:rope");
    }

    private static CharacterEntity CreateEditableCharacter()
    {
        return new CharacterEntity
        {
            Id = Guid.NewGuid(),
            OwnerUserId = "user-1",
            RaceId = "human",
            ClassId = "rogue",
            BackgroundId = "criminal",
            Name = "Нокс",
            Race = "Человек",
            ClassName = "Плут",
            Background = "Преступник",
            Level = 1,
            HitPoints = 8,
            MaxHitPoints = 8,
            CurrentHitPoints = 8,
            Speed = 30,
            ProficiencyBonus = 2,
            PassivePerception = 10,
            AbilitiesJson = "[]",
            BaseAbilitiesJson = "[]",
            BonusAbilitySelectionsJson = "[]",
            SkillsJson = "[]",
            KnownSpellsJson = "[]",
            SpellSlotsJson = "[]",
            SpentSpellSlotsJson = "{}",
            InventoryJson = "[]",
            CalculationTraceJson = "[]"
        };
    }
}

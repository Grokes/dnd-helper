using Xunit;

namespace dnd_helper.Tests.Features.Rooms;

public sealed class RoomCombatServiceTests
{
    [Fact]
    public void AttackCharacter_HitRollsDamageAndReducesHitPoints()
    {
        var service = CreateService(15, 4, 3);
        var monster = CreateMonster(attackBonus: 4, damageDice: "2d6", damageBonus: 2);
        var target = CreateTarget(armorClass: 16, currentHitPoints: 20);

        var outcome = service.AttackCharacter(monster, target);

        Assert.True(outcome.IsSuccess);
        Assert.NotNull(outcome.Result);
        Assert.True(outcome.Result.Result.IsHit);
        Assert.Equal(19, outcome.Result.Result.AttackTotal);
        Assert.Equal(7, outcome.Result.Result.DamageDiceResult);
        Assert.Equal(9, outcome.Result.Result.DamageTotal);
        Assert.Equal(11, outcome.Result.NextTargetCurrentHitPoints);
    }

    [Fact]
    public void AttackCharacter_MissDoesNotRollDamage()
    {
        var service = CreateService(5, 6);
        var monster = CreateMonster(attackBonus: 1, damageDice: "1d8", damageBonus: 3);
        var target = CreateTarget(armorClass: 18, currentHitPoints: 20);

        var outcome = service.AttackCharacter(monster, target);

        Assert.True(outcome.IsSuccess);
        Assert.NotNull(outcome.Result);
        Assert.False(outcome.Result.Result.IsHit);
        Assert.Equal(6, outcome.Result.Result.AttackTotal);
        Assert.Equal(0, outcome.Result.Result.DamageTotal);
        Assert.Equal(20, outcome.Result.NextTargetCurrentHitPoints);
    }

    [Fact]
    public void AttackCharacter_NaturalTwentyHitsEvenWhenArmorClassIsHigher()
    {
        var service = CreateService(20, 2);
        var monster = CreateMonster(attackBonus: 0, damageDice: "1d4", damageBonus: 0);
        var target = CreateTarget(armorClass: 30, currentHitPoints: 10);

        var outcome = service.AttackCharacter(monster, target);

        Assert.True(outcome.IsSuccess);
        Assert.NotNull(outcome.Result);
        Assert.True(outcome.Result.Result.IsCriticalHit);
        Assert.True(outcome.Result.Result.IsHit);
        Assert.Equal(8, outcome.Result.NextTargetCurrentHitPoints);
    }

    [Fact]
    public void RollMonsterDamage_RejectsInvalidDiceExpression()
    {
        var service = CreateService();
        var monster = CreateMonster(damageDice: "плохая кость");

        var outcome = service.RollMonsterDamage(monster);

        Assert.False(outcome.IsSuccess);
        Assert.NotNull(outcome.Errors);
        Assert.Contains("damageDice", outcome.Errors.Keys);
    }

    private static RoomCombatService CreateService(params int[] rolls)
    {
        var index = 0;
        var diceRoller = new DiceRoller((minimumInclusive, maximumExclusive) =>
        {
            if (rolls.Length == 0)
            {
                return minimumInclusive;
            }

            var value = rolls[Math.Min(index, rolls.Length - 1)];
            index++;
            return Math.Clamp(value, minimumInclusive, maximumExclusive - 1);
        });

        return new RoomCombatService(diceRoller);
    }

    private static EncounterCombatantEntity CreateMonster(
        int attackBonus = 0,
        string damageDice = "1d4",
        int damageBonus = 0)
    {
        return new EncounterCombatantEntity
        {
            Id = Guid.NewGuid(),
            Name = "Гоблин",
            AttackName = "Скимитар",
            AttackBonus = attackBonus,
            DamageDice = damageDice,
            DamageBonus = damageBonus
        };
    }

    private static CharacterEntity CreateTarget(int armorClass, int currentHitPoints)
    {
        return new CharacterEntity
        {
            Id = Guid.NewGuid(),
            Name = "Воин",
            ArmorClass = armorClass,
            HitPoints = 20,
            MaxHitPoints = 20,
            CurrentHitPoints = currentHitPoints
        };
    }
}

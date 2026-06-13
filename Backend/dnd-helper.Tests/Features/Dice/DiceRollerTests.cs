using Xunit;

namespace dnd_helper.Tests.Features.Dice;

public sealed class DiceRollerTests
{
    [Fact]
    public void TryRoll_RollsDiceExpressionWithModifier()
    {
        var roller = CreateRoller(2, 5);

        var success = roller.TryRoll("2d6+3", out var result);

        Assert.True(success);
        Assert.Equal([2, 5], result.Rolls);
        Assert.Equal(3, result.Modifier);
        Assert.Equal(10, result.Total);
        Assert.Equal("2d6 (2 + 5) + 3", result.Expression);
    }

    [Fact]
    public void TryRoll_RollsFlatExpressionWithNegativeModifier()
    {
        var roller = CreateRoller();

        var success = roller.TryRoll("7-2", out var result);

        Assert.True(success);
        Assert.Empty(result.Rolls);
        Assert.Equal(-2, result.Modifier);
        Assert.Equal(5, result.Total);
        Assert.Equal("7 - 2", result.Expression);
    }

    [Fact]
    public void TryRoll_RejectsInvalidExpression()
    {
        var roller = CreateRoller();

        var success = roller.TryRoll("огненный кубик", out var result);

        Assert.False(success);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public void RollD20_ReturnsNaturalFlagsAndTotal()
    {
        var roller = CreateRoller(20);

        var result = roller.RollD20(5);

        Assert.Equal(20, result.Roll);
        Assert.Equal(25, result.Total);
        Assert.True(result.IsNaturalTwenty);
        Assert.False(result.IsNaturalOne);
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

using System.Text.RegularExpressions;

namespace dnd_helper.Application.Common.Dice;

public sealed record DiceRollResult(
    string Source,
    string Expression,
    IReadOnlyList<int> Rolls,
    int Modifier,
    int Total);

public sealed record D20RollResult(
    int Roll,
    int Modifier,
    int Total,
    bool IsNaturalOne,
    bool IsNaturalTwenty);

public sealed class DiceRoller
{
    private readonly Func<int, int, int> rollNext;

    public DiceRoller()
        : this((minimumInclusive, maximumExclusive) => Random.Shared.Next(minimumInclusive, maximumExclusive))
    {
    }

    public DiceRoller(Func<int, int, int> rollNext)
    {
        this.rollNext = rollNext;
    }

    public D20RollResult RollD20(int modifier = 0)
    {
        var roll = rollNext(1, 21);
        return new D20RollResult(
            roll,
            modifier,
            roll + modifier,
            roll == 1,
            roll == 20);
    }

    public bool TryRoll(string sourceDice, out DiceRollResult result)
    {
        result = new DiceRollResult(sourceDice, sourceDice, [], 0, 0);

        if (string.IsNullOrWhiteSpace(sourceDice))
        {
            return false;
        }

        var normalized = sourceDice.Trim().ToLowerInvariant().Replace(" ", string.Empty);
        var match = Regex.Match(
            normalized,
            @"^(?:(?<count>\d+)d(?<sides>\d+)|(?<flat>\d+))(?:(?<sign>[+-])(?<bonus>\d+))?$");
        if (!match.Success)
        {
            return false;
        }

        var modifier = ParseModifier(match);
        if (modifier is < -10_000 or > 10_000)
        {
            return false;
        }

        if (match.Groups["flat"].Success)
        {
            var flatValue = int.Parse(match.Groups["flat"].Value);
            if (flatValue < 0 || flatValue > 10_000)
            {
                return false;
            }

            result = new DiceRollResult(
                sourceDice,
                FormatFlatExpression(flatValue, modifier),
                [],
                modifier,
                Math.Max(0, flatValue + modifier));
            return true;
        }

        if (!match.Groups["count"].Success || !match.Groups["sides"].Success)
        {
            return false;
        }

        var count = int.Parse(match.Groups["count"].Value);
        var sides = int.Parse(match.Groups["sides"].Value);
        if (count <= 0 || sides <= 0 || count > 30 || sides > 1000)
        {
            return false;
        }

        var rolls = new List<int>(count);
        for (var index = 0; index < count; index++)
        {
            rolls.Add(rollNext(1, sides + 1));
        }

        var sum = rolls.Sum();
        result = new DiceRollResult(
            sourceDice,
            FormatDiceExpression(count, sides, rolls, modifier),
            rolls,
            modifier,
            Math.Max(0, sum + modifier));
        return true;
    }

    private static int ParseModifier(Match match)
    {
        if (!match.Groups["bonus"].Success)
        {
            return 0;
        }

        var modifier = int.Parse(match.Groups["bonus"].Value);
        return match.Groups["sign"].Value == "-" ? -modifier : modifier;
    }

    private static string FormatFlatExpression(int value, int modifier)
    {
        return modifier switch
        {
            > 0 => $"{value} + {modifier}",
            < 0 => $"{value} - {Math.Abs(modifier)}",
            _ => value.ToString()
        };
    }

    private static string FormatDiceExpression(int count, int sides, IReadOnlyList<int> rolls, int modifier)
    {
        var baseExpression = count > 1
            ? $"{count}d{sides} ({string.Join(" + ", rolls)})"
            : $"{count}d{sides} ({rolls[0]})";

        return modifier switch
        {
            > 0 => $"{baseExpression} + {modifier}",
            < 0 => $"{baseExpression} - {Math.Abs(modifier)}",
            _ => baseExpression
        };
    }
}

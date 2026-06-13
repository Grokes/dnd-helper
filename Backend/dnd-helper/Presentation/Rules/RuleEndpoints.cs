using dnd_helper.Infrastructure.Seeding;

namespace dnd_helper.Presentation.Rules;

public static class RuleEndpoints
{
    private sealed record RuleSpellDto(
        string Slug,
        string Name,
        IReadOnlyList<string> ClassSlugs,
        int SpellLevel,
        int MinCharacterLevel,
        string Summary,
        string Description,
        string? DamageDice,
        string? DamageType);

    private sealed record RuleConditionDto(
        string Slug,
        string Name,
        string Description);

    public static IEndpointRouteBuilder MapRuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/rulesets", async (IRulesCatalogRepository repository, CancellationToken cancellationToken) =>
            Results.Ok(await repository.GetRulesetsAsync(cancellationToken)));

        endpoints.MapGet("/api/rules/races", async (IRulesCatalogRepository repository, CancellationToken cancellationToken) =>
            Results.Ok(await repository.GetRacesAsync(RulesDatabaseSeeder.RulesetId, cancellationToken)));

        endpoints.MapGet("/api/rules/classes", async (IRulesCatalogRepository repository, CancellationToken cancellationToken) =>
            Results.Ok(await repository.GetClassesAsync(RulesDatabaseSeeder.RulesetId, cancellationToken)));

        endpoints.MapGet("/api/rules/backgrounds", async (IRulesCatalogRepository repository, CancellationToken cancellationToken) =>
            Results.Ok(await repository.GetBackgroundsAsync(RulesDatabaseSeeder.RulesetId, cancellationToken)));

        endpoints.MapGet("/api/rules/features", async (IRulesCatalogRepository repository, CancellationToken cancellationToken) =>
            Results.Ok(await repository.GetFeaturesAsync(RulesDatabaseSeeder.RulesetId, cancellationToken)));

        endpoints.MapGet("/api/rules/spells", async (IRulesCatalogRepository repository, CancellationToken cancellationToken) =>
        {
            var spells = await repository.GetSpellsAsync(RulesDatabaseSeeder.RulesetId, cancellationToken);
            var dto = spells.Select(spell =>
            {
                var summary = spell.Effects.FirstOrDefault(effect => effect.Target.Equals("summary", StringComparison.OrdinalIgnoreCase))?.Reason
                    ?? spell.Name;
                var description = spell.Effects.FirstOrDefault(effect => effect.Target.Equals("description", StringComparison.OrdinalIgnoreCase))?.Reason
                    ?? "Описание будет добавлено позже.";
                TryGetSpellDamageProfile(spell.Slug, out var damageDice, out var damageType);
                if (damageDice is null)
                {
                    TryInferSpellDamage(summary, description, out damageDice, out damageType);
                }
                return new RuleSpellDto(
                    spell.Slug,
                    spell.Name,
                    spell.ClassSlugs,
                    spell.SpellLevel,
                    spell.MinCharacterLevel,
                    summary,
                    description,
                    damageDice,
                    damageType);
            }).ToList();
            return Results.Ok(dto);
        });

        endpoints.MapGet("/api/rules/conditions", async (IRulesCatalogRepository repository, CancellationToken cancellationToken) =>
        {
            var conditions = await repository.GetConditionsAsync(RulesDatabaseSeeder.RulesetId, cancellationToken);
            var dto = conditions.Select(condition => new RuleConditionDto(
                condition.Slug,
                condition.Name,
                condition.Effects.FirstOrDefault(effect => effect.Target.Equals("description", StringComparison.OrdinalIgnoreCase))?.Reason
                    ?? "Описание состояния будет добавлено позже."))
                .ToList();
            return Results.Ok(dto);
        });

        return endpoints;
    }

    private static bool TryGetSpellDamageProfile(string slug, out string? damageDice, out string? damageType)
    {
        var map = new Dictionary<string, (string dice, string type)>(StringComparer.OrdinalIgnoreCase)
        {
            ["fire-bolt"] = ("1d10", "огонь"),
            ["chill-touch"] = ("1d8", "некротический"),
            ["ray-of-frost"] = ("1d8", "холод"),
            ["sacred-flame"] = ("1d8", "сияние"),
            ["thorn-whip"] = ("1d6", "колющий"),
            ["poison-spray"] = ("1d12", "яд"),
            ["burning-hands"] = ("3d6", "огонь"),
            ["thunderwave"] = ("2d8", "гром"),
            ["magic-missile"] = ("3d4+3", "силовой"),
            ["chromatic-orb"] = ("3d8", "элементальный"),
            ["guiding-bolt"] = ("4d6", "сияние"),
            ["inflict-wounds"] = ("3d10", "некротический"),
            ["witch-bolt"] = ("1d12", "молния"),
            ["scorching-ray"] = ("2d6", "огонь"),
            ["shatter"] = ("3d8", "гром"),
            ["melfs-acid-arrow"] = ("4d4", "кислота"),
            ["fireball"] = ("8d6", "огонь"),
            ["lightning-bolt"] = ("8d6", "молния"),
            ["blight"] = ("8d8", "некротический"),
            ["cone-of-cold"] = ("8d8", "холод")
        };

        if (map.TryGetValue(slug, out var profile))
        {
            damageDice = profile.dice;
            damageType = profile.type;
            return true;
        }

        damageDice = null;
        damageType = null;
        return false;
    }

    private static bool TryInferSpellDamage(string summary, string description, out string? damageDice, out string? damageType)
    {
        var text = $"{summary} {description}";
        var diceMatch = System.Text.RegularExpressions.Regex.Match(text, @"(?<dice>\d+d\d+(\s*[\+\-]\s*\d+)?)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!diceMatch.Success)
        {
            damageDice = null;
            damageType = null;
            return false;
        }

        damageDice = diceMatch.Groups["dice"].Value.Replace(" ", string.Empty);
        var normalized = text.ToLowerInvariant();
        damageType = normalized.Contains("огн") ? "огонь"
            : normalized.Contains("холод") ? "холод"
            : normalized.Contains("молн") ? "молния"
            : normalized.Contains("гром") ? "гром"
            : normalized.Contains("кисл") ? "кислота"
            : normalized.Contains("яд") ? "яд"
            : normalized.Contains("некрот") ? "некротический"
            : normalized.Contains("сиян") ? "сияние"
            : normalized.Contains("псих") ? "психический"
            : normalized.Contains("силов") ? "силовой"
            : "урон";
        return true;
    }
}

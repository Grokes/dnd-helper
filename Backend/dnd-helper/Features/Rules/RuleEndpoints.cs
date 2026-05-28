using dnd_helper.Infrastructure.Seeding;

namespace dnd_helper.Features.Rules;

public static class RuleEndpoints
{
    private sealed record RuleSpellDto(
        string Slug,
        string Name,
        IReadOnlyList<string> ClassSlugs,
        int SpellLevel,
        int MinCharacterLevel,
        string Summary,
        string Description);

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
            var dto = spells.Select(spell => new RuleSpellDto(
                spell.Slug,
                spell.Name,
                spell.ClassSlugs,
                spell.SpellLevel,
                spell.MinCharacterLevel,
                spell.Effects.FirstOrDefault(effect => effect.Target.Equals("summary", StringComparison.OrdinalIgnoreCase))?.Reason
                    ?? spell.Name,
                spell.Effects.FirstOrDefault(effect => effect.Target.Equals("description", StringComparison.OrdinalIgnoreCase))?.Reason
                    ?? "Описание будет добавлено позже."))
                .ToList();
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
}

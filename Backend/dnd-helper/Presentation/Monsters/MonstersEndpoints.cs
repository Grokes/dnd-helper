using dnd_helper.Infrastructure.Seeding;

namespace dnd_helper.Presentation.Monsters;

public static class MonstersEndpoints
{
    public static IEndpointRouteBuilder MapMonstersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/monsters", async (IRulesCatalogRepository repository, CancellationToken cancellationToken) =>
            Results.Ok(await repository.GetMonstersAsync(RulesDatabaseSeeder.RulesetId, cancellationToken)));

        return endpoints;
    }
}

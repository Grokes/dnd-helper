using dnd_helper.Features.Rules;
using dnd_helper.Infrastructure.Seeding;

namespace dnd_helper.Features.Monsters;

public static class MonstersEndpoints
{
    public static IEndpointRouteBuilder MapMonstersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/monsters", async (IRulesCatalogRepository repository, CancellationToken cancellationToken) =>
            Results.Ok(await repository.GetMonstersAsync(RulesDatabaseSeeder.RulesetId, cancellationToken)));

        return endpoints;
    }
}

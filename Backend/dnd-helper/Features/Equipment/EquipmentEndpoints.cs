using dnd_helper.Features.Rules;
using dnd_helper.Infrastructure.Seeding;

namespace dnd_helper.Features.Equipment;

public static class EquipmentEndpoints
{
    public static IEndpointRouteBuilder MapEquipmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/equipment", async (IRulesCatalogRepository repository, CancellationToken cancellationToken) =>
            Results.Ok(await repository.GetEquipmentAsync(RulesDatabaseSeeder.RulesetId, cancellationToken)));

        return endpoints;
    }
}

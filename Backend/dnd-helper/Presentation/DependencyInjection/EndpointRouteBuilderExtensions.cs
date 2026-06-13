namespace dnd_helper.Presentation.DependencyInjection;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAuthEndpoints();
        endpoints.MapCharacterEndpoints();
        endpoints.MapRoomEndpoints();
        endpoints.MapRuleEndpoints();
        endpoints.MapEquipmentEndpoints();
        endpoints.MapMonstersEndpoints();
        endpoints.MapLegacyReferenceEndpoints();

        return endpoints;
    }
}

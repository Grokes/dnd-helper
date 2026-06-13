using Microsoft.Extensions.DependencyInjection;

namespace dnd_helper.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<DiceRoller>();
        services.AddScoped<RuleResolutionService>();
        services.AddScoped<CharacterCreationService>();
        services.AddScoped<CharacterRestService>();
        services.AddScoped<CharacterSpellService>();
        services.AddScoped<RoomAccessService>();
        services.AddScoped<RoomMonsterService>();

        return services;
    }
}

using dnd_helper.Application.Rooms.UseCases;
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
        services.AddScoped<CreateCharacterUseCase>();
        services.AddScoped<UpdateCharacterUseCase>();
        services.AddScoped<RestCharacterUseCase>();
        services.AddScoped<CastCharacterSpellUseCase>();
        services.AddScoped<ListRoomsUseCase>();
        services.AddScoped<CreateRoomUseCase>();
        services.AddScoped<GetRoomUseCase>();
        services.AddScoped<JoinRoomUseCase>();
        services.AddScoped<JoinRoomByInviteUseCase>();
        services.AddScoped<SelectRoomCharacterUseCase>();
        services.AddScoped<UpdateRoomPresenceUseCase>();
        services.AddScoped<UpdateRoomMemberRoleUseCase>();
        services.AddScoped<RoomCombatService>();
        services.AddScoped<RoomInitiativeService>();
        services.AddScoped<ListRoomMonstersUseCase>();
        services.AddScoped<AddRoomMonsterUseCase>();
        services.AddScoped<ApplyRoomMonsterDamageUseCase>();
        services.AddScoped<DeleteRoomMonsterUseCase>();
        services.AddScoped<AttackWithRoomMonsterUseCase>();
        services.AddScoped<RollRoomMonsterDamageUseCase>();
        services.AddScoped<StartRoomCombatUseCase>();
        services.AddScoped<EndRoomCombatUseCase>();
        services.AddScoped<FinishRoomTurnUseCase>();
        services.AddScoped<RoomAccessService>();
        services.AddScoped<RoomMonsterService>();

        return services;
    }
}

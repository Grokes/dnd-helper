using dnd_helper.Application.Rooms.UseCases;
using dnd_helper.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace dnd_helper.Application.Rooms;

public sealed class RoomInitiativeService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext dbContext;
    private readonly DiceRoller diceRoller;

    public RoomInitiativeService(AppDbContext dbContext, DiceRoller diceRoller)
    {
        this.dbContext = dbContext;
        this.diceRoller = diceRoller;
    }

    public async Task<UseCaseResult<RoomDto>> StartCombatAsync(
        Guid roomId,
        string currentUserId,
        CancellationToken cancellationToken)
    {
        var room = await LoadRoomAsync(roomId, cancellationToken);
        if (room is null)
        {
            return UseCaseResult<RoomDto>.NotFound();
        }

        var encounter = EnsureEncounter(room);
        var selectedCharacters = room.Members
            .SelectMany(member => member.Characters)
            .Where(link => link.Character is not null)
            .Select(link => link.Character!)
            .DistinctBy(character => character.Id)
            .ToList();

        var monsters = encounter.Combatants
            .Where(combatant => !combatant.IsPlayerCharacter && combatant.CurrentHitPoints > 0)
            .ToList();

        if (selectedCharacters.Count == 0 && monsters.Count == 0)
        {
            return UseCaseResult<RoomDto>.ValidationFailed(new Dictionary<string, string[]>
            {
                ["combat"] = ["Перед началом боя добавь в комнату персонажа или чудовище."]
            });
        }

        dbContext.EncounterCombatants.RemoveRange(encounter.Combatants.Where(combatant => combatant.IsPlayerCharacter));
        encounter.Combatants.RemoveAll(combatant => combatant.IsPlayerCharacter);

        foreach (var character in selectedCharacters)
        {
            var initiativeModifier = GetAbilityModifier(character, "DEX");
            var initiativeRoll = diceRoller.RollD20(initiativeModifier);
            var combatant = new EncounterCombatantEntity
            {
                Id = Guid.NewGuid(),
                EncounterId = encounter.Id,
                Encounter = encounter,
                CharacterId = character.Id,
                Name = character.Name,
                Initiative = initiativeRoll.Total,
                ArmorClass = character.ArmorClass,
                MaxHitPoints = character.MaxHitPoints <= 0 ? character.HitPoints : character.MaxHitPoints,
                CurrentHitPoints = character.CurrentHitPoints <= 0
                    ? (character.MaxHitPoints <= 0 ? character.HitPoints : character.MaxHitPoints)
                    : character.CurrentHitPoints,
                IsPlayerCharacter = true
            };
            dbContext.EncounterCombatants.Add(combatant);
        }

        foreach (var monster in monsters)
        {
            monster.Initiative = diceRoller.RollD20().Total;
        }

        var turnOrder = BuildTurnOrder(encounter);
        encounter.IsCombatActive = true;
        encounter.RoundNumber = 1;
        encounter.CurrentTurnCombatantId = turnOrder.First().Id;
        encounter.TurnStartedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return UseCaseResult<RoomDto>.Success(room.ToDto(currentUserId));
    }

    public async Task<UseCaseResult<RoomDto>> EndCombatAsync(
        Guid roomId,
        string currentUserId,
        CancellationToken cancellationToken)
    {
        var room = await LoadRoomAsync(roomId, cancellationToken);
        if (room is null)
        {
            return UseCaseResult<RoomDto>.NotFound();
        }

        var encounter = GetPrimaryEncounter(room);
        if (encounter is null)
        {
            return UseCaseResult<RoomDto>.Success(room.ToDto(currentUserId));
        }

        encounter.IsCombatActive = false;
        encounter.RoundNumber = 0;
        encounter.CurrentTurnCombatantId = null;
        encounter.TurnStartedAtUtc = null;

        dbContext.EncounterCombatants.RemoveRange(encounter.Combatants.Where(combatant => combatant.IsPlayerCharacter));
        encounter.Combatants.RemoveAll(combatant => combatant.IsPlayerCharacter);

        await dbContext.SaveChangesAsync(cancellationToken);
        return UseCaseResult<RoomDto>.Success(room.ToDto(currentUserId));
    }

    public async Task<UseCaseResult<RoomDto>> FinishCurrentTurnAsync(
        Guid roomId,
        string currentUserId,
        bool canManageRoom,
        CancellationToken cancellationToken)
    {
        var room = await LoadRoomAsync(roomId, cancellationToken);
        if (room is null)
        {
            return UseCaseResult<RoomDto>.NotFound();
        }

        var encounter = GetPrimaryEncounter(room);
        if (encounter is not { IsCombatActive: true })
        {
            return UseCaseResult<RoomDto>.ValidationFailed(new Dictionary<string, string[]>
            {
                ["combat"] = ["Бой ещё не начат."]
            });
        }

        var turnOrder = BuildTurnOrder(encounter);
        if (turnOrder.Count == 0)
        {
            encounter.IsCombatActive = false;
            encounter.RoundNumber = 0;
            encounter.CurrentTurnCombatantId = null;
            encounter.TurnStartedAtUtc = null;
            await dbContext.SaveChangesAsync(cancellationToken);
            return UseCaseResult<RoomDto>.Success(room.ToDto(currentUserId));
        }

        var currentIndex = Math.Max(0, turnOrder.FindIndex(combatant => combatant.Id == encounter.CurrentTurnCombatantId));
        var currentCombatant = turnOrder[currentIndex];
        if (!canManageRoom && !IsOwnedByUser(room, currentCombatant, currentUserId))
        {
            return UseCaseResult<RoomDto>.Forbidden();
        }

        var nextIndex = (currentIndex + 1) % turnOrder.Count;
        if (nextIndex == 0)
        {
            encounter.RoundNumber = Math.Max(1, encounter.RoundNumber) + 1;
        }

        encounter.CurrentTurnCombatantId = turnOrder[nextIndex].Id;
        encounter.TurnStartedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return UseCaseResult<RoomDto>.Success(room.ToDto(currentUserId));
    }

    private Task<RoomEntity?> LoadRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        return dbContext.Rooms
            .IncludeRoomGraph()
            .FirstOrDefaultAsync(room => room.Id == roomId, cancellationToken);
    }

    private EncounterEntity EnsureEncounter(RoomEntity room)
    {
        var encounter = GetPrimaryEncounter(room);
        if (encounter is not null)
        {
            return encounter;
        }

        encounter = new EncounterEntity
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            Name = "Основная сцена",
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.Encounters.Add(encounter);
        room.Encounters.Add(encounter);
        return encounter;
    }

    private static EncounterEntity? GetPrimaryEncounter(RoomEntity room)
    {
        return room.Encounters
            .OrderByDescending(encounter => encounter.IsCombatActive)
            .ThenByDescending(encounter => encounter.CreatedAtUtc)
            .FirstOrDefault();
    }

    private static List<EncounterCombatantEntity> BuildTurnOrder(EncounterEntity encounter)
    {
        return encounter.Combatants
            .Where(combatant => combatant.CurrentHitPoints > 0)
            .DistinctBy(combatant => combatant.Id)
            .OrderByDescending(combatant => combatant.Initiative)
            .ThenByDescending(combatant => combatant.IsPlayerCharacter)
            .ThenBy(combatant => combatant.Name)
            .ToList();
    }

    private static bool IsOwnedByUser(RoomEntity room, EncounterCombatantEntity combatant, string userId)
    {
        if (!combatant.IsPlayerCharacter || combatant.CharacterId is not { } characterId)
        {
            return false;
        }

        return room.Members.Any(member =>
            member.UserId == userId &&
            member.Characters.Any(link => link.CharacterId == characterId));
    }

    private static int GetAbilityModifier(CharacterEntity character, string abilityKey)
    {
        if (character.Abilities.Count > 0)
        {
            return character.Abilities
                .FirstOrDefault(ability => ability.Ability.Equals(abilityKey, StringComparison.OrdinalIgnoreCase))
                ?.Modifier ?? 0;
        }

        try
        {
            var abilities = JsonSerializer.Deserialize<List<AbilityScoreDto>>(character.AbilitiesJson, JsonOptions) ?? [];
            return abilities.FirstOrDefault(ability => ability.Key.Equals(abilityKey, StringComparison.OrdinalIgnoreCase))?.Modifier ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}

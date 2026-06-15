using dnd_helper.Infrastructure.Identity;
using Xunit;

namespace dnd_helper.Tests.Domain.Rooms;

public sealed class RoomEntityTests
{
    [Fact]
    public void ToDto_IncludesActiveCombatStateAndOrderedInitiative()
    {
        var roomId = Guid.NewGuid();
        var userId = "user-1";
        var characterId = Guid.NewGuid();
        var characterCombatantId = Guid.NewGuid();
        var monsterCombatantId = Guid.NewGuid();

        var room = new RoomEntity
        {
            Id = roomId,
            Name = "Комната",
            OwnerUserId = userId,
            OwnerUser = new ApplicationUser { Id = userId, DisplayName = "Ведущий" },
            Members =
            [
                new RoomMembershipEntity
                {
                    RoomId = roomId,
                    UserId = userId,
                    Role = RoomMemberRoles.GameMaster,
                    User = new ApplicationUser { Id = userId, DisplayName = "Ведущий" },
                    Characters =
                    [
                        new RoomMembershipCharacterEntity
                        {
                            RoomId = roomId,
                            UserId = userId,
                            CharacterId = characterId,
                            Character = new CharacterEntity
                            {
                                Id = characterId,
                                Name = "Мира",
                                Race = "Эльф",
                                ClassName = "Воин",
                                Level = 1,
                                ArmorClass = 14,
                                MaxHitPoints = 12,
                                CurrentHitPoints = 12
                            }
                        }
                    ]
                }
            ],
            Encounters =
            [
                new EncounterEntity
                {
                    Id = Guid.NewGuid(),
                    RoomId = roomId,
                    IsCombatActive = true,
                    RoundNumber = 2,
                    CurrentTurnCombatantId = characterCombatantId,
                    Combatants =
                    [
                        new EncounterCombatantEntity
                        {
                            Id = monsterCombatantId,
                            Name = "Гоблин",
                            Initiative = 9,
                            ArmorClass = 15,
                            MaxHitPoints = 7,
                            CurrentHitPoints = 7,
                            IsPlayerCharacter = false
                        },
                        new EncounterCombatantEntity
                        {
                            Id = characterCombatantId,
                            CharacterId = characterId,
                            Name = "Мира",
                            Initiative = 14,
                            ArmorClass = 14,
                            MaxHitPoints = 12,
                            CurrentHitPoints = 12,
                            IsPlayerCharacter = true
                        }
                    ]
                }
            ]
        };

        var dto = room.ToDto(userId);

        Assert.True(dto.Combat.IsActive);
        Assert.Equal(2, dto.Combat.RoundNumber);
        Assert.Equal(characterCombatantId, dto.Combat.CurrentCombatantId);
        Assert.Equal("Мира", dto.Combat.CurrentCombatant?.Name);
        Assert.Equal(userId, dto.Combat.CurrentCombatant?.OwnerUserId);
        Assert.Equal([characterCombatantId, monsterCombatantId], dto.Combat.TurnOrder.Select(item => item.Id).ToList());
    }
}

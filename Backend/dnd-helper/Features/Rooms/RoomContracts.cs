using dnd_helper.Features.Characters;

namespace dnd_helper.Features.Rooms;

public sealed record CreateRoomRequest(string Name);

public sealed record JoinRoomRequest(string JoinCode);

public sealed record JoinRoomByInviteRequest(string InviteToken);

public sealed record SelectRoomCharacterRequest(Guid? CharacterId);

public sealed record UpdateRoomMemberRoleRequest(string Role);
public sealed record AddRoomMonsterRequest(string MonsterSlug);
public sealed record ApplyMonsterDamageRequest(int Damage);

public sealed record RoomSummaryDto(
    Guid Id,
    string Name,
    string JoinCode,
    string InviteToken,
    string OwnerDisplayName,
    int MemberCount,
    int ConnectedMemberCount,
    string CurrentUserRole,
    bool IsOwner);

public sealed record RoomMemberCharacterDto(Guid Id, string Name, string Race, string ClassName, int Level);

public sealed record RoomMemberDto(
    string UserId,
    string DisplayName,
    string Role,
    bool IsOwner,
    bool IsOnline,
    DateTime JoinedAtUtc,
    List<RoomMemberCharacterDto> Characters,
    List<string> Inventory);

public sealed record RoomDto(
    Guid Id,
    string Name,
    string JoinCode,
    string InviteToken,
    string OwnerDisplayName,
    string CurrentUserRole,
    bool CanManageMembers,
    int ConnectedMembers,
    List<RoomMemberDto> Members);

public sealed record RoomMonsterDto(
    Guid Id,
    string MonsterSlug,
    string Name,
    decimal ChallengeRating,
    int ArmorClass,
    int MaxHitPoints,
    int CurrentHitPoints,
    string AttackName,
    int AttackBonus,
    string DamageDice,
    int DamageBonus,
    string DamageType);

public sealed record MonsterDamageRollDto(
    Guid MonsterId,
    string MonsterName,
    string AttackName,
    string DamageExpression,
    int DiceResult,
    int DamageBonus,
    int TotalDamage,
    DateTime RolledAtUtc);

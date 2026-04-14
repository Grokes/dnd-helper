using dnd_helper.Features.Characters;

namespace dnd_helper.Features.Rooms;

public sealed record CreateRoomRequest(string Name);

public sealed record JoinRoomRequest(string JoinCode);

public sealed record JoinRoomByInviteRequest(string InviteToken);

public sealed record SelectRoomCharacterRequest(Guid? CharacterId);

public sealed record UpdateRoomMemberRoleRequest(string Role);

public sealed record UpdateRoomSessionRequest(string? ActiveMemberUserId);

public sealed record RoomSummaryDto(
    Guid Id,
    string Name,
    string JoinCode,
    string InviteToken,
    string OwnerDisplayName,
    int MemberCount,
    int ConnectedMemberCount,
    string CurrentUserRole,
    bool IsOwner,
    string? ActiveMemberDisplayName,
    string? ActiveCharacterName);

public sealed record RoomMemberCharacterDto(Guid Id, string Name, string Race, string ClassName, int Level);

public sealed record RoomMemberDto(
    string UserId,
    string DisplayName,
    string Role,
    bool IsOwner,
    bool IsOnline,
    DateTime JoinedAtUtc,
    RoomMemberCharacterDto? Character);

public sealed record RoomSessionDto(
    string? ActiveMemberUserId,
    string? ActiveMemberDisplayName,
    string? ActiveCharacterName,
    DateTime? UpdatedAtUtc,
    int ConnectedMembers);

public sealed record RoomDto(
    Guid Id,
    string Name,
    string JoinCode,
    string InviteToken,
    string OwnerDisplayName,
    string CurrentUserRole,
    bool CanManageMembers,
    bool CanManageSession,
    RoomSessionDto Session,
    List<RoomMemberDto> Members);

using dnd_helper.Features.Auth;
using dnd_helper.Features.Characters;

namespace dnd_helper.Features.Rooms;

public static class RoomMemberRoles
{
    public const string GameMaster = "GameMaster";
    public const string Player = "Player";
}

public sealed class RoomEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string JoinCode { get; set; } = string.Empty;
    public string InviteToken { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public ApplicationUser? OwnerUser { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? ActiveMemberUserId { get; set; }
    public DateTime? SessionUpdatedAtUtc { get; set; }
    public List<RoomMembershipEntity> Members { get; set; } = [];

    public RoomSummaryDto ToSummaryDto(string currentUserId)
    {
        var currentMembership = Members.FirstOrDefault(member => member.UserId == currentUserId);
        var activeMember = Members.FirstOrDefault(member => member.UserId == ActiveMemberUserId);

        return new RoomSummaryDto(
            Id,
            Name,
            JoinCode,
            InviteToken,
            OwnerUser?.DisplayName ?? "Мастер",
            Members.Count,
            Members.Count(IsMemberOnline),
            currentMembership?.Role ?? RoomMemberRoles.Player,
            OwnerUserId == currentUserId,
            activeMember?.User?.DisplayName,
            activeMember?.Character?.Name);
    }

    public RoomDto ToDto(string currentUserId)
    {
        var currentMembership = Members.FirstOrDefault(member => member.UserId == currentUserId);
        var canManageRoom = currentMembership?.Role == RoomMemberRoles.GameMaster || OwnerUserId == currentUserId;
        var activeMember = Members.FirstOrDefault(member => member.UserId == ActiveMemberUserId);

        return new RoomDto(
            Id,
            Name,
            JoinCode,
            InviteToken,
            OwnerUser?.DisplayName ?? "Мастер",
            currentMembership?.Role ?? RoomMemberRoles.Player,
            canManageRoom,
            canManageRoom,
            new RoomSessionDto(
                ActiveMemberUserId,
                activeMember?.User?.DisplayName,
                activeMember?.Character?.Name,
                SessionUpdatedAtUtc,
                Members.Count(IsMemberOnline)),
            Members
                .OrderByDescending(member => member.Role == RoomMemberRoles.GameMaster)
                .ThenBy(member => member.User?.DisplayName ?? member.UserId)
                .Select(member => new RoomMemberDto(
                    member.UserId,
                    member.User?.DisplayName ?? "Игрок",
                    member.Role,
                    member.UserId == OwnerUserId,
                    IsMemberOnline(member),
                    member.JoinedAtUtc,
                    member.Character is null
                        ? null
                        : new RoomMemberCharacterDto(
                            member.Character.Id,
                            member.Character.Name,
                            member.Character.Race,
                            member.Character.ClassName,
                            member.Character.Level)))
                .ToList());
    }

    private static bool IsMemberOnline(RoomMembershipEntity member)
    {
        if (member.LastSeenAtUtc is null)
        {
            return false;
        }

        return member.LastSeenAtUtc.Value >= DateTime.UtcNow.AddMinutes(-2);
    }
}

public sealed class RoomMembershipEntity
{
    public Guid RoomId { get; set; }
    public RoomEntity? Room { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public Guid? CharacterId { get; set; }
    public CharacterEntity? Character { get; set; }
    public string Role { get; set; } = RoomMemberRoles.Player;
    public DateTime JoinedAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
}

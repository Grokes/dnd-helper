using System.Text.Json;

namespace dnd_helper.Domain.Rooms;

public static class RoomMemberRoles
{
    public const string GameMaster = "GameMaster";
    public const string Player = "Player";
}

public sealed class RoomEntity
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string JoinCode { get; set; } = string.Empty;
    public string InviteToken { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public ApplicationUser? OwnerUser { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<RoomMembershipEntity> Members { get; set; } = [];

    public RoomSummaryDto ToSummaryDto(string currentUserId)
    {
        var currentMembership = Members.FirstOrDefault(member => member.UserId == currentUserId);
        return new RoomSummaryDto(
            Id,
            Name,
            JoinCode,
            InviteToken,
            OwnerUser?.DisplayName ?? "Мастер",
            Members.Count,
            Members.Count(IsMemberOnline),
            currentMembership?.Role ?? RoomMemberRoles.Player,
            OwnerUserId == currentUserId);
    }

    public RoomDto ToDto(string currentUserId)
    {
        var currentMembership = Members.FirstOrDefault(member => member.UserId == currentUserId);
        var canManageRoom = currentMembership?.Role == RoomMemberRoles.GameMaster || OwnerUserId == currentUserId;
        return new RoomDto(
            Id,
            Name,
            JoinCode,
            InviteToken,
            OwnerUser?.DisplayName ?? "Мастер",
            currentMembership?.Role ?? RoomMemberRoles.Player,
            canManageRoom,
            Members.Count(IsMemberOnline),
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
                    member.Characters
                        .Select(characterLink => new RoomMemberCharacterDto(
                            characterLink.Character!.Id,
                            characterLink.Character.Name,
                            characterLink.Character.Race,
                            characterLink.Character.ClassName,
                            characterLink.Character.Level,
                            characterLink.Character.ArmorClass,
                            characterLink.Character.MaxHitPoints <= 0 ? characterLink.Character.HitPoints : characterLink.Character.MaxHitPoints,
                            characterLink.Character.CurrentHitPoints <= 0
                                ? (characterLink.Character.MaxHitPoints <= 0 ? characterLink.Character.HitPoints : characterLink.Character.MaxHitPoints)
                                : characterLink.Character.CurrentHitPoints))
                        .ToList(),
                    DeserializeInventory(member.InventoryJson)))
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

    private static List<string> DeserializeInventory(string source)
    {
        return JsonSerializer.Deserialize<List<string>>(source, JsonOptions) ?? [];
    }
}

public sealed class RoomMembershipEntity
{
    public Guid RoomId { get; set; }
    public RoomEntity? Room { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public List<RoomMembershipCharacterEntity> Characters { get; set; } = [];
    public string Role { get; set; } = RoomMemberRoles.Player;
    public string InventoryJson { get; set; } = "[]";
    public DateTime JoinedAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
}

public sealed class RoomMembershipCharacterEntity
{
    public Guid RoomId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid CharacterId { get; set; }
    public RoomMembershipEntity? Membership { get; set; }
    public CharacterEntity? Character { get; set; }
}

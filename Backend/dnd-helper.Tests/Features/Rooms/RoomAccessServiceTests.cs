using Xunit;

namespace dnd_helper.Tests.Features.Rooms;

public sealed class RoomAccessServiceTests
{
    [Fact]
    public void CanManageRoom_ReturnsTrueForGameMaster()
    {
        var membership = CreateMembership(role: RoomMemberRoles.GameMaster, ownerUserId: "owner", userId: "player");

        var canManage = RoomAccessService.CanManageRoom(membership, "player");

        Assert.True(canManage);
    }

    [Fact]
    public void CanManageRoom_ReturnsTrueForOwner()
    {
        var membership = CreateMembership(role: RoomMemberRoles.Player, ownerUserId: "owner", userId: "owner");

        var canManage = RoomAccessService.CanManageRoom(membership, "owner");

        Assert.True(canManage);
    }

    [Fact]
    public void CanManageRoom_ReturnsFalseForRegularPlayer()
    {
        var membership = CreateMembership(role: RoomMemberRoles.Player, ownerUserId: "owner", userId: "player");

        var canManage = RoomAccessService.CanManageRoom(membership, "player");

        Assert.False(canManage);
    }

    [Fact]
    public void CanManageRoom_ReturnsFalseWhenMembershipIsMissing()
    {
        var canManage = RoomAccessService.CanManageRoom(null, "player");

        Assert.False(canManage);
    }

    private static RoomMembershipEntity CreateMembership(string role, string ownerUserId, string userId)
    {
        return new RoomMembershipEntity
        {
            UserId = userId,
            Role = role,
            Room = new RoomEntity
            {
                Id = Guid.NewGuid(),
                OwnerUserId = ownerUserId
            }
        };
    }
}

using ChatWithMe.Application.Common;
using ChatWithMe.Application.Friendships;
using ChatWithMe.Domain.Friendships;
using ChatWithMe.Infrastructure.Common;
using ChatWithMe.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatWithMe.Tests;

public sealed class FriendAppServiceTests
{
    private readonly FakeFriendshipRepository _friendships = new();
    private readonly FakeFriendRequestRepository _requests = new();
    private readonly FakeBlockRepository _blocks = new();
    private readonly FakeUserRepository _users = new();
    private readonly FriendAppService _sut;

    public FriendAppServiceTests()
    {
        _sut = new FriendAppService(
            _friendships,
            _requests,
            _blocks,
            _users,
            new SystemClock(),
            NullLogger<FriendAppService>.Instance);
    }

    [Fact]
    public async Task SendRequest_ToSelf_Throws()
    {
        var user = _users.Add("self@example.com");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.SendRequestAsync(user.Id, new SendFriendRequestRequest(user.Id, null), CancellationToken.None));
    }

    [Fact]
    public async Task SendRequest_UnknownTarget_ThrowsNotFound()
    {
        var user = _users.Add("a1@example.com");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.SendRequestAsync(user.Id, new SendFriendRequestRequest(Guid.NewGuid(), null), CancellationToken.None));
    }

    [Fact]
    public async Task SendRequest_WhenAlreadyFriends_ThrowsConflict()
    {
        var userA = _users.Add("a2@example.com");
        var userB = _users.Add("b2@example.com");
        await _friendships.AddAsync(new Friendship(userA.Id, userB.Id, DateTime.UtcNow), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.SendRequestAsync(userA.Id, new SendFriendRequestRequest(userB.Id, null), CancellationToken.None));
    }

    [Fact]
    public async Task SendRequest_WhenPendingExists_ThrowsConflict()
    {
        var userA = _users.Add("a3@example.com");
        var userB = _users.Add("b3@example.com");

        await _sut.SendRequestAsync(userA.Id, new SendFriendRequestRequest(userB.Id, "hi"), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.SendRequestAsync(userA.Id, new SendFriendRequestRequest(userB.Id, "again"), CancellationToken.None));

        // 反向重复申请同样冲突。
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.SendRequestAsync(userB.Id, new SendFriendRequestRequest(userA.Id, "reverse"), CancellationToken.None));
    }

    [Fact]
    public async Task SendRequest_WhenBlocked_ThrowsForbidden()
    {
        var userA = _users.Add("a4@example.com");
        var userB = _users.Add("b4@example.com");
        await _blocks.AddAsync(new UserBlock(userB.Id, userA.Id, DateTime.UtcNow), CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.SendRequestAsync(userA.Id, new SendFriendRequestRequest(userB.Id, null), CancellationToken.None));
    }

    [Fact]
    public async Task Accept_CreatesBidirectionalFriendship()
    {
        var userA = _users.Add("a5@example.com");
        var userB = _users.Add("b5@example.com");
        var created = await _sut.SendRequestAsync(userA.Id, new SendFriendRequestRequest(userB.Id, "hi"), CancellationToken.None);

        var accepted = await _sut.AcceptAsync(userB.Id, created.Id, CancellationToken.None);

        Assert.Equal(FriendRequestStatus.Accepted, accepted.Status);
        Assert.Equal(2, _friendships.Rows.Count);
        Assert.True(await _friendships.ExistsAsync(userA.Id, userB.Id, CancellationToken.None));
        Assert.True(await _friendships.ExistsAsync(userB.Id, userA.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Accept_ByRequester_ThrowsForbidden()
    {
        var userA = _users.Add("a6@example.com");
        var userB = _users.Add("b6@example.com");
        var created = await _sut.SendRequestAsync(userA.Id, new SendFriendRequestRequest(userB.Id, null), CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.AcceptAsync(userA.Id, created.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Accept_Twice_ThrowsConflict()
    {
        var userA = _users.Add("a7@example.com");
        var userB = _users.Add("b7@example.com");
        var created = await _sut.SendRequestAsync(userA.Id, new SendFriendRequestRequest(userB.Id, null), CancellationToken.None);

        await _sut.AcceptAsync(userB.Id, created.Id, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.AcceptAsync(userB.Id, created.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Reject_DoesNotCreateFriendship()
    {
        var userA = _users.Add("a8@example.com");
        var userB = _users.Add("b8@example.com");
        var created = await _sut.SendRequestAsync(userA.Id, new SendFriendRequestRequest(userB.Id, null), CancellationToken.None);

        var rejected = await _sut.RejectAsync(userB.Id, created.Id, CancellationToken.None);

        Assert.Equal(FriendRequestStatus.Rejected, rejected.Status);
        Assert.Empty(_friendships.Rows);
    }

    [Fact]
    public async Task RemoveFriend_DeletesBothRows()
    {
        var userA = _users.Add("a9@example.com");
        var userB = _users.Add("b9@example.com");
        await _friendships.AddAsync(new Friendship(userA.Id, userB.Id, DateTime.UtcNow), CancellationToken.None);
        await _friendships.AddAsync(new Friendship(userB.Id, userA.Id, DateTime.UtcNow), CancellationToken.None);

        await _sut.RemoveFriendAsync(userA.Id, userB.Id, CancellationToken.None);

        Assert.Empty(_friendships.Rows);
    }

    [Fact]
    public async Task SetRemark_IsVisibleInFriendList()
    {
        var userA = _users.Add("a10@example.com");
        var userB = _users.Add("b10@example.com");
        await _friendships.AddAsync(new Friendship(userA.Id, userB.Id, DateTime.UtcNow), CancellationToken.None);

        await _sut.SetRemarkAsync(userA.Id, userB.Id, "  老同学  ", CancellationToken.None);

        var friends = await _sut.ListFriendsAsync(userA.Id, CancellationToken.None);
        var friend = Assert.Single(friends);
        Assert.Equal("老同学", friend.Remark);
    }

    [Fact]
    public async Task Block_RemovesFriendship()
    {
        var userA = _users.Add("a11@example.com");
        var userB = _users.Add("b11@example.com");

        await _friendships.AddAsync(new Friendship(userA.Id, userB.Id, DateTime.UtcNow), CancellationToken.None);
        await _friendships.AddAsync(new Friendship(userB.Id, userA.Id, DateTime.UtcNow), CancellationToken.None);

        await _sut.BlockAsync(userA.Id, userB.Id, CancellationToken.None);

        Assert.Empty(_friendships.Rows);
        Assert.True(await _blocks.IsBlockedAsync(userA.Id, userB.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Search_ExcludesSelf()
    {
        var me = _users.Add("me12@example.com");
        _users.Add("other12@example.com");

        var result = await _sut.SearchUsersAsync(me.Id, "example.com", CancellationToken.None);

        Assert.DoesNotContain(result, user => user.Id == me.Id);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task Search_EmptyKeyword_ReturnsEmpty()
    {
        _users.Add("x13@example.com");

        var result = await _sut.SearchUsersAsync(Guid.NewGuid(), "   ", CancellationToken.None);

        Assert.Empty(result);
    }
}

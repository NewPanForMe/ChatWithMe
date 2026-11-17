using ChatWithMe.Domain.Friendships;

namespace ChatWithMe.Tests;

public sealed class FriendDomainTests
{
    [Fact]
    public void FriendRequest_ToSelf_Throws()
    {
        var user = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => FriendRequest.Create(user, user, null));
    }

    [Fact]
    public void FriendRequest_TooLongMessage_Throws()
    {
        var message = new string('x', FriendRequest.MaxMessageLength + 1);

        Assert.Throws<ArgumentException>(() => FriendRequest.Create(Guid.NewGuid(), Guid.NewGuid(), message));
    }

    [Fact]
    public void FriendRequest_AcceptTwice_Throws()
    {
        var request = FriendRequest.Create(Guid.NewGuid(), Guid.NewGuid(), null);
        request.Accept(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => request.Accept(DateTime.UtcNow));
    }

    [Fact]
    public void FriendRequest_RejectAfterAccept_Throws()
    {
        var request = FriendRequest.Create(Guid.NewGuid(), Guid.NewGuid(), null);
        request.Accept(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => request.Reject(DateTime.UtcNow));
    }

    [Fact]
    public void Friendship_WithSelf_Throws()
    {
        var user = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new Friendship(user, user, DateTime.UtcNow));
    }

    [Fact]
    public void Friendship_RemarkTooLong_Throws()
    {
        var friendship = new Friendship(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => friendship.SetRemark(new string('x', Friendship.MaxRemarkLength + 1)));
    }

    [Fact]
    public void Friendship_BlankRemark_BecomesNull()
    {
        var friendship = new Friendship(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        friendship.SetRemark("   ");

        Assert.Null(friendship.Remark);
    }

    [Fact]
    public void UserBlock_WithSelf_Throws()
    {
        var user = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new UserBlock(user, user, DateTime.UtcNow));
    }
}

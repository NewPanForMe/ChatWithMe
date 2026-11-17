using ChatWithMe.Domain.Conversations;
using ChatWithMe.Domain.Users;

namespace ChatWithMe.Tests;

public sealed class ConversationTests
{
    [Fact]
    public void CreateDirect_AssignsOwnerToInitiator()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var conversation = Conversation.CreateDirect(userA, userB);

        Assert.Equal(ConversationType.Direct, conversation.Type);
        Assert.Equal(2, conversation.Members.Count);
        Assert.Equal(ConversationMemberRole.Owner, conversation.FindMember(userA)!.Role);
        Assert.Equal(ConversationMemberRole.Member, conversation.FindMember(userB)!.Role);
    }

    [Fact]
    public void CreateDirect_WithSameUser_Throws()
    {
        var user = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => Conversation.CreateDirect(user, user));
    }

    [Fact]
    public void CreateGroup_RequiresTitle()
    {
        Assert.Throws<ArgumentException>(() => Conversation.CreateGroup("   ", Guid.NewGuid()));
    }

    [Fact]
    public void CreateGroup_RejectsTooLongTitle()
    {
        var title = new string('x', Conversation.MaxTitleLength + 1);

        Assert.Throws<ArgumentException>(() => Conversation.CreateGroup(title, Guid.NewGuid()));
    }

    [Fact]
    public void CreateGroup_ContainsOnlyCreator()
    {
        var creator = Guid.NewGuid();

        var conversation = Conversation.CreateGroup("测试群", creator);

        Assert.Equal(ConversationType.Group, conversation.Type);
        Assert.Equal("测试群", conversation.Title);
        Assert.Single(conversation.Members);
        Assert.Equal(ConversationMemberRole.Owner, conversation.FindMember(creator)!.Role);
    }

    [Fact]
    public void AddMember_IsIdempotent()
    {
        var creator = Guid.NewGuid();
        var member = Guid.NewGuid();
        var conversation = Conversation.CreateGroup("群", creator);

        var first = conversation.AddMember(member);
        var second = conversation.AddMember(member);

        Assert.Same(first, second);
        Assert.Equal(2, conversation.Members.Count);
    }

    [Fact]
    public void RemoveMember_MissingMember_Throws()
    {
        var conversation = Conversation.CreateGroup("群", Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => conversation.RemoveMember(Guid.NewGuid()));
    }

    [Fact]
    public void EnsureMember_NonMember_Throws()
    {
        var conversation = Conversation.CreateGroup("群", Guid.NewGuid());

        Assert.Throws<UnauthorizedAccessException>(() => conversation.EnsureMember(Guid.NewGuid()));
    }

    [Fact]
    public void UserRole_DefaultsToMember()
    {
        var user = User.Register("role@example.com", "角色", "hash");

        Assert.Equal(UserRole.Member, user.Role);
    }
}

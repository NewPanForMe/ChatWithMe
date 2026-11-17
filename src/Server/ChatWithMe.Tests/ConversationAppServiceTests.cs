using ChatWithMe.Application.Common;
using ChatWithMe.Application.Conversations;
using ChatWithMe.Domain.Conversations;
using ChatWithMe.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatWithMe.Tests;

public sealed class ConversationAppServiceTests
{
    private readonly FakeConversationRepository _conversations = new();
    private readonly FakeBlockRepository _blocks = new();
    private readonly FakeUserRepository _users = new();
    private readonly ConversationAppService _sut;

    public ConversationAppServiceTests()
    {
        _sut = new ConversationAppService(_conversations, _blocks, _users, NullLogger<ConversationAppService>.Instance);
    }

    [Fact]
    public async Task CreateDirect_ReusesExistingConversation()
    {
        var owner = _users.Add("a@example.com");
        var peer = _users.Add("b@example.com");

        var first = await _sut.CreateAsync(owner.Id, new CreateConversationRequest(ConversationType.Direct, peer.Id, null), CancellationToken.None);
        var second = await _sut.CreateAsync(owner.Id, new CreateConversationRequest(ConversationType.Direct, peer.Id, null), CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(_conversations.All);
        Assert.Equal(2, first.Members.Count);
    }

    [Fact]
    public async Task CreateDirect_UnknownPeer_ThrowsNotFound()
    {
        var owner = _users.Add("c@example.com");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.CreateAsync(owner.Id, new CreateConversationRequest(ConversationType.Direct, Guid.NewGuid(), null), CancellationToken.None));
    }

    [Fact]
    public async Task CreateDirect_WithSelf_Throws()
    {
        var owner = _users.Add("d@example.com");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.CreateAsync(owner.Id, new CreateConversationRequest(ConversationType.Direct, owner.Id, null), CancellationToken.None));
    }

    [Fact]
    public async Task CreateGroup_ContainsOnlyCreator()
    {
        var owner = _users.Add("e@example.com");

        var conversation = await _sut.CreateAsync(owner.Id, new CreateConversationRequest(ConversationType.Group, null, "项目群"), CancellationToken.None);

        Assert.Equal(ConversationType.Group, conversation.Type);
        Assert.Equal("项目群", conversation.Title);
        Assert.Single(conversation.Members);
        Assert.Equal(owner.Id, conversation.Members[0].UserId);
    }

    [Fact]
    public async Task Get_NonMember_ThrowsForbidden()
    {
        var owner = _users.Add("f@example.com");
        var peer = _users.Add("g@example.com");
        var outsider = _users.Add("h@example.com");

        var conversation = await _sut.CreateAsync(owner.Id, new CreateConversationRequest(ConversationType.Direct, peer.Id, null), CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.GetAsync(outsider.Id, conversation.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Get_MissingConversation_ThrowsNotFound()
    {
        var user = _users.Add("i@example.com");

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetAsync(user.Id, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task List_ReturnsOnlyConversationsOfUser()
    {
        var owner = _users.Add("j@example.com");
        var peer = _users.Add("k@example.com");
        var stranger = _users.Add("l@example.com");

        await _sut.CreateAsync(owner.Id, new CreateConversationRequest(ConversationType.Direct, peer.Id, null), CancellationToken.None);

        var ownerList = await _sut.ListAsync(owner.Id, CancellationToken.None);
        var strangerList = await _sut.ListAsync(stranger.Id, CancellationToken.None);

        Assert.Single(ownerList);
        Assert.Empty(strangerList);
    }
}

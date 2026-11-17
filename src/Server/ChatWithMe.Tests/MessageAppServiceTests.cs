using ChatWithMe.Application.Common;
using ChatWithMe.Application.Messages;
using ChatWithMe.Domain.Conversations;
using ChatWithMe.Domain.Messages;
using ChatWithMe.Domain.Users;
using ChatWithMe.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatWithMe.Tests;

public sealed class MessageAppServiceTests
{
    private readonly FakeMessageRepository _messages = new();
    private readonly FakeConversationRepository _conversations = new();
    private readonly FakeBlockRepository _blocks = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeChatNotifier _notifier = new();
    private readonly MessageAppService _sut;

    public MessageAppServiceTests()
    {
        _sut = new MessageAppService(_messages, _conversations, _blocks, _users, _notifier, NullLogger<MessageAppService>.Instance);
    }

    private async Task<(User Owner, User Peer, Conversation Conversation)> ArrangeDirectAsync()
    {
        var owner = _users.Add("owner@example.com");
        var peer = _users.Add("peer@example.com");
        var conversation = Conversation.CreateDirect(owner.Id, peer.Id);
        await _conversations.AddAsync(conversation, CancellationToken.None);

        return (owner, peer, conversation);
    }

    [Fact]
    public async Task Send_NonMember_ThrowsForbidden()
    {
        var (_, _, conversation) = await ArrangeDirectAsync();
        var outsider = _users.Add("outsider@example.com");

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.SendAsync(outsider.Id, new SendMessageRequest(conversation.Id, "hi"), CancellationToken.None));

        Assert.Contains("不是该会话成员", exception.Message);
    }

    [Fact]
    public async Task Send_UnknownConversation_ThrowsNotFound()
    {
        var user = _users.Add("user@example.com");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.SendAsync(user.Id, new SendMessageRequest(Guid.NewGuid(), "hi"), CancellationToken.None));
    }

    [Fact]
    public async Task Send_Member_StoresMessage()
    {
        var (owner, _, conversation) = await ArrangeDirectAsync();

        var dto = await _sut.SendAsync(owner.Id, new SendMessageRequest(conversation.Id, "hello"), CancellationToken.None);

        Assert.Single(_messages.Messages);
        Assert.Equal("hello", dto.Content);
        Assert.Equal(conversation.Id, dto.ConversationId);
    }

    [Fact]
    public async Task Send_WithDuplicatedClientMessageId_ReturnsExistingMessage()
    {
        var (owner, _, conversation) = await ArrangeDirectAsync();
        var existing = Message.Send(conversation.Id, owner.Id, "hello", "client-1");
        _messages.ExistingOnAdd = existing;

        var dto = await _sut.SendAsync(
            owner.Id,
            new SendMessageRequest(conversation.Id, "hello", "client-1"),
            CancellationToken.None);

        Assert.Equal(existing.Id, dto.Id);
        Assert.Empty(_messages.Messages);
    }

    [Fact]
    public async Task Get_NonMember_ThrowsForbidden()
    {
        var (_, _, conversation) = await ArrangeDirectAsync();
        var outsider = _users.Add("reader@example.com");

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.GetAsync(outsider.Id, conversation.Id, null, 20, CancellationToken.None));
    }

    [Fact]
    public async Task Get_UnknownConversation_ThrowsNotFound()
    {
        var user = _users.Add("reader2@example.com");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetAsync(user.Id, Guid.NewGuid(), null, 20, CancellationToken.None));
    }

    [Fact]
    public async Task Get_Member_ReturnsMessagesInDescendingOrder()
    {
        var (owner, peer, conversation) = await ArrangeDirectAsync();

        await _sut.SendAsync(owner.Id, new SendMessageRequest(conversation.Id, "one"), CancellationToken.None);
        await _sut.SendAsync(peer.Id, new SendMessageRequest(conversation.Id, "two"), CancellationToken.None);

        var page = await _sut.GetAsync(peer.Id, conversation.Id, null, 20, CancellationToken.None);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal("two", page.Items[0].Content);
        Assert.NotNull(page.Next);
    }

    [Fact]
    public async Task Get_SameTimestampMessages_PaginateWithoutGapsOrDuplicates()
    {
        var (owner, peer, conversation) = await ArrangeDirectAsync();
        var sentAt = new DateTime(2026, 11, 17, 1, 2, 3, 456, DateTimeKind.Utc);

        for (var index = 0; index < 5; index++)
        {
            _messages.Messages.Add(Message.Restore(
                Guid.NewGuid(),
                conversation.Id,
                owner.Id,
                "same-ms-" + index,
                null,
                sentAt));
        }

        var collected = new List<Guid>();
        MessageCursor? cursor = null;

        for (var pageIndex = 0; pageIndex < 3; pageIndex++)
        {
            var page = await _sut.GetAsync(peer.Id, conversation.Id, cursor, 2, CancellationToken.None);

            collected.AddRange(page.Items.Select(item => item.Id));
            cursor = page.Next;
        }

        Assert.Equal(5, collected.Count);
        Assert.Equal(5, collected.Distinct().Count());
    }

    [Fact]
    public async Task Get_AdminCanReadConversationWithoutMembership()
    {
        var (_, _, conversation) = await ArrangeDirectAsync();
        var admin = _users.Add("admin@example.com", "管理员", UserRole.Admin);

        var page = await _sut.GetAsync(admin.Id, conversation.Id, null, 20, CancellationToken.None);

        Assert.Empty(page.Items);
    }
}

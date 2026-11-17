using ChatWithMe.Application.Messages;
using ChatWithMe.Application.Realtime;
using ChatWithMe.Domain.Admin;
using ChatWithMe.Domain.Auth;
using ChatWithMe.Domain.Conversations;
using ChatWithMe.Domain.Friendships;
using ChatWithMe.Domain.Messages;
using ChatWithMe.Domain.Users;

namespace ChatWithMe.Tests.Fakes;

internal sealed class FakeConversationRepository : IConversationRepository
{
    private readonly Dictionary<Guid, Conversation> _conversations = [];

    public IReadOnlyCollection<Conversation> All => _conversations.Values;

    public Task AddAsync(Conversation conversation, CancellationToken cancellationToken)
    {
        _conversations[conversation.Id] = conversation;
        return Task.CompletedTask;
    }

    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_conversations.GetValueOrDefault(id));

    public Task<IReadOnlyList<Conversation>> ListForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Conversation>>(
            _conversations.Values.Where(conversation => conversation.IsMember(userId)).ToList());

    public Task<Conversation?> FindDirectAsync(Guid userA, Guid userB, CancellationToken cancellationToken) =>
        Task.FromResult(_conversations.Values.FirstOrDefault(conversation =>
            conversation.Type == ConversationType.Direct
            && conversation.Members.Count == 2
            && conversation.IsMember(userA)
            && conversation.IsMember(userB)));

    public Task<ConversationMember?> GetMembershipAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_conversations.GetValueOrDefault(conversationId)?.FindMember(userId));

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public User Add(string email, string nickName = "user", UserRole role = UserRole.Member)
    {
        var user = User.Register(email, nickName, "hash", role);
        _users.Add(user);

        return user;
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_users.FirstOrDefault(user => user.Id == id));

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(_users.FirstOrDefault(user => user.Email.Value == email));

    public Task<bool> ExistsWithEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(_users.Any(user => user.Email.Value == email));

    public Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<User>>(_users.Where(user => ids.Contains(user.Id)).ToList());

    public Task<IReadOnlyList<User>> ListAsync(int skip, int take, string? keyword, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<User>>(_users.Skip(skip).Take(take).ToList());

    public Task<int> CountAsync(string? keyword, CancellationToken cancellationToken) =>
        Task.FromResult(_users.Count);

    public Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        _users.RemoveAll(user => user.Id == userId);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakeMessageRepository : IMessageRepository
{
    public List<Message> Messages { get; } = [];

    /// <summary>模拟幂等命中：非空时 AddAsync 直接返回该消息，不写入新记录。</summary>
    public Message? ExistingOnAdd { get; set; }

    public Task<Message> AddAsync(Message message, CancellationToken cancellationToken)
    {
        if (ExistingOnAdd is not null)
        {
            return Task.FromResult(ExistingOnAdd);
        }

        Messages.Add(message);

        return Task.FromResult(message);
    }

    public Task<Message?> GetByClientMessageIdAsync(Guid conversationId, string clientMessageId, CancellationToken cancellationToken) =>
        Task.FromResult<Message?>(Messages.FirstOrDefault(message =>
            message.ConversationId == conversationId && message.ClientMessageId == clientMessageId));

    public Task<IReadOnlyList<Message>> GetByConversationAsync(
        Guid conversationId,
        MessageCursor? before,
        int limit,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Message>>(
            Messages
                .Where(message => message.ConversationId == conversationId)
                .Where(message => IsAfterCursor(message, before))
                .OrderByDescending(message => message.SentAtUtc)
                .ThenByDescending(message => message.Id.ToString(), StringComparer.Ordinal)
                .Take(limit)
                .ToList());

    private static bool IsAfterCursor(Message message, MessageCursor? before)
    {
        if (before is null)
        {
            return true;
        }

        if (message.SentAtUtc != before.SentAtUtc)
        {
            return message.SentAtUtc < before.SentAtUtc;
        }

        if (before.MessageId is null)
        {
            return false;
        }

        return string.CompareOrdinal(message.Id.ToString(), before.MessageId.Value.ToString()) < 0;
    }
}

internal sealed class FakeFriendshipRepository : IFriendshipRepository
{
    private readonly List<Friendship> _rows = [];

    public IReadOnlyList<Friendship> Rows => _rows;

    public Task<bool> ExistsAsync(Guid userId, Guid friendId, CancellationToken cancellationToken) =>
        Task.FromResult(_rows.Any(row => row.UserId == userId && row.FriendId == friendId));

    public Task<IReadOnlyList<Friendship>> ListByUserAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Friendship>>(_rows.Where(row => row.UserId == userId).ToList());

    public Task<Friendship?> GetAsync(Guid userId, Guid friendId, CancellationToken cancellationToken) =>
        Task.FromResult(_rows.FirstOrDefault(row => row.UserId == userId && row.FriendId == friendId));

    public Task AddAsync(Friendship friendship, CancellationToken cancellationToken)
    {
        _rows.Add(friendship);
        return Task.CompletedTask;
    }

    public Task RemovePairAsync(Guid userA, Guid userB, CancellationToken cancellationToken)
    {
        _rows.RemoveAll(row =>
            (row.UserId == userA && row.FriendId == userB) || (row.UserId == userB && row.FriendId == userA));

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakeFriendRequestRepository : IFriendRequestRepository
{
    public List<FriendRequest> Requests { get; } = [];

    public Task AddAsync(FriendRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.CompletedTask;
    }

    public Task<FriendRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Requests.FirstOrDefault(request => request.Id == id));

    public Task<FriendRequest?> GetPendingBetweenAsync(Guid userA, Guid userB, CancellationToken cancellationToken) =>
        Task.FromResult(Requests.FirstOrDefault(request =>
            request.Status == FriendRequestStatus.Pending
            && ((request.RequesterId == userA && request.AddresseeId == userB)
                || (request.RequesterId == userB && request.AddresseeId == userA))));

    public Task<IReadOnlyList<FriendRequest>> ListIncomingPendingAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FriendRequest>>(Requests
            .Where(request => request.AddresseeId == userId && request.Status == FriendRequestStatus.Pending)
            .ToList());

    public Task<IReadOnlyList<FriendRequest>> ListOutgoingAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FriendRequest>>(Requests
            .Where(request => request.RequesterId == userId)
            .ToList());

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakeBlockRepository : IBlockRepository
{    public List<UserBlock> Blocks { get; } = [];

    public Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken) =>
        Task.FromResult(Blocks.Any(block => block.BlockerId == blockerId && block.BlockedId == blockedId));

    public Task<bool> IsBlockedEitherWayAsync(Guid userA, Guid userB, CancellationToken cancellationToken) =>
        Task.FromResult(Blocks.Any(block =>
            (block.BlockerId == userA && block.BlockedId == userB)
            || (block.BlockerId == userB && block.BlockedId == userA)));

    public Task<IReadOnlyList<UserBlock>> ListByBlockerAsync(Guid blockerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserBlock>>(Blocks.Where(block => block.BlockerId == blockerId).ToList());

    public Task AddAsync(UserBlock block, CancellationToken cancellationToken)
    {
        Blocks.Add(block);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken)
    {
        Blocks.RemoveAll(block => block.BlockerId == blockerId && block.BlockedId == blockedId);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakeChatNotifier : IChatNotifier
{
    public List<MessageDto> Notified { get; } = [];

    public Task MessageReceivedAsync(MessageDto message, CancellationToken cancellationToken)
    {
        Notified.Add(message);
        return Task.CompletedTask;
    }
}

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = [];

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        Tokens.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Tokens.FirstOrDefault(token => token.TokenHash == tokenHash));

    public Task<int> RevokeAllForUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var active = Tokens
            .Where(token => token.UserId == userId && token.IsActive(utcNow))
            .ToList();

        foreach (var token in active)
        {
            token.Revoke(utcNow, null);
        }

        return Task.FromResult(active.Count);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakeOnlineUserRegistry : IOnlineUserRegistry
{
    private readonly Dictionary<Guid, HashSet<string>> _connections = [];

    public void Connect(Guid userId, string connectionId)
    {
        if (!_connections.TryGetValue(userId, out var set))
        {
            set = [];
            _connections[userId] = set;
        }

        set.Add(connectionId);
    }

    public void Disconnect(Guid userId, string connectionId)
    {
        if (_connections.TryGetValue(userId, out var set))
        {
            set.Remove(connectionId);

            if (set.Count == 0)
            {
                _connections.Remove(userId);
            }
        }
    }

    public bool IsOnline(Guid userId) => _connections.ContainsKey(userId);

    public int GetConnectionCount(Guid userId) =>
        _connections.TryGetValue(userId, out var set) ? set.Count : 0;

    public IReadOnlyCollection<Guid> GetOnlineUserIds() => _connections.Keys.ToList();
}

internal sealed class FakeSessionTerminator : IOnlineSessionTerminator
{
    public List<Guid> Terminated { get; } = [];

    public Task<int> TerminateAsync(Guid userId, CancellationToken cancellationToken)
    {
        Terminated.Add(userId);
        return Task.FromResult(1);
    }
}

internal sealed class FakeAdminUserRepository : IAdminUserRepository
{
    private readonly List<AdminUser> _admins = [];

    public AdminUser Add(string email, string displayName = "管理员")
    {
        var admin = AdminUser.Create(email, displayName, "hash");
        _admins.Add(admin);

        return admin;
    }

    public Task AddAsync(AdminUser user, CancellationToken cancellationToken)
    {
        _admins.Add(user);
        return Task.CompletedTask;
    }

    public Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_admins.FirstOrDefault(admin => admin.Id == id));

    public Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(_admins.FirstOrDefault(admin => admin.Email == email));

    public Task<IReadOnlyList<AdminUser>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AdminUser>>(_admins.ToList());

    public Task<bool> AnyAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_admins.Count > 0);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _admins.RemoveAll(admin => admin.Id == id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

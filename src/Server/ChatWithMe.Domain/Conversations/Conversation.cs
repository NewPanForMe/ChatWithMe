namespace ChatWithMe.Domain.Conversations;

public sealed class Conversation
{
    public const int MaxTitleLength = 100;

    public const int MaxMembers = 500;

    private readonly List<ConversationMember> _members = [];

    private Conversation()
    {
    }

    private Conversation(Guid id, ConversationType type, string? title, Guid createdByUserId, DateTime createdAtUtc)
    {
        Id = id;
        Type = type;
        Title = title;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public ConversationType Type { get; private set; }

    public string? Title { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<ConversationMember> Members => _members.AsReadOnly();

    public static Conversation CreateDirect(Guid userA, Guid userB)
    {
        if (userA == Guid.Empty || userB == Guid.Empty)
        {
            throw new ArgumentException("单聊双方标识不能为空。");
        }

        if (userA == userB)
        {
            throw new ArgumentException("不能与自己建立单聊。", nameof(userB));
        }

        var conversation = new Conversation(Guid.NewGuid(), ConversationType.Direct, null, userA, DateTime.UtcNow);

        conversation.AddMemberInternal(userA, ConversationMemberRole.Owner);
        conversation.AddMemberInternal(userB, ConversationMemberRole.Member);

        return conversation;
    }

    public static Conversation CreateGroup(string title, Guid creatorId, IEnumerable<Guid>? memberIds = null)
    {
        if (creatorId == Guid.Empty)
        {
            throw new ArgumentException("群主标识不能为空。", nameof(creatorId));
        }

        var normalizedTitle = (title ?? string.Empty).Trim();

        if (normalizedTitle.Length == 0)
        {
            throw new ArgumentException("群名称不能为空。", nameof(title));
        }

        if (normalizedTitle.Length > MaxTitleLength)
        {
            throw new ArgumentException("群名称长度不能超过 " + MaxTitleLength + " 个字符。", nameof(title));
        }

        var conversation = new Conversation(Guid.NewGuid(), ConversationType.Group, normalizedTitle, creatorId, DateTime.UtcNow);

        conversation.AddMemberInternal(creatorId, ConversationMemberRole.Owner);

        foreach (var memberId in memberIds ?? [])
        {
            if (memberId != creatorId)
            {
                conversation.AddMemberInternal(memberId, ConversationMemberRole.Member);
            }
        }

        if (conversation._members.Count > MaxMembers)
        {
            throw new ArgumentException("群成员数量不能超过 " + MaxMembers + " 人。", nameof(memberIds));
        }

        return conversation;
    }

    public ConversationMember? FindMember(Guid userId) => _members.Find(member => member.UserId == userId);

    public bool IsMember(Guid userId) => FindMember(userId) is not null;

    public ConversationMember AddMember(Guid userId, ConversationMemberRole role = ConversationMemberRole.Member)
    {
        var existing = FindMember(userId);

        if (existing is not null)
        {
            return existing;
        }

        return AddMemberInternal(userId, role);
    }

    public void RemoveMember(Guid userId)
    {
        var member = FindMember(userId) ?? throw new InvalidOperationException("该用户不在会话中。");

        _members.Remove(member);
    }

    private ConversationMember AddMemberInternal(Guid userId, ConversationMemberRole role)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("成员标识不能为空。", nameof(userId));
        }

        var member = new ConversationMember(Id, userId, role, DateTime.UtcNow);
        _members.Add(member);

        return member;
    }

    /// <summary>供基础设施层从数据库恢复成员集合（EF Core 也通过导航属性写入同一字段）。</summary>
    internal void RestoreMember(ConversationMember member) => _members.Add(member);

    public void EnsureMember(Guid userId)
    {
        if (!IsMember(userId))
        {
            throw new UnauthorizedAccessException("不是该会话成员。");
        }
    }
}

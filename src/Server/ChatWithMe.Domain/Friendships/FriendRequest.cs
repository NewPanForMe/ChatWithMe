using ChatWithMe.Domain.Common;

namespace ChatWithMe.Domain.Friendships;

public sealed class FriendRequest : Entity
{
    public const int MaxMessageLength = 200;

    private FriendRequest()
    {
    }

    private FriendRequest(Guid id, Guid requesterId, Guid addresseeId, string? message, DateTime createdAtUtc) : base(id)
    {
        RequesterId = requesterId;
        AddresseeId = addresseeId;
        Message = message;
        Status = FriendRequestStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid RequesterId { get; private set; }

    public Guid AddresseeId { get; private set; }

    public string? Message { get; private set; }

    public FriendRequestStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? RespondedAtUtc { get; private set; }

    public static FriendRequest Create(Guid requesterId, Guid addresseeId, string? message)
    {
        if (requesterId == Guid.Empty || addresseeId == Guid.Empty)
        {
            throw new ArgumentException("好友申请双方标识不能为空。");
        }

        if (requesterId == addresseeId)
        {
            throw new ArgumentException("不能添加自己为好友。", nameof(addresseeId));
        }

        var normalized = string.IsNullOrWhiteSpace(message) ? null : message.Trim();

        if (normalized is not null && normalized.Length > MaxMessageLength)
        {
            throw new ArgumentException("申请留言长度不能超过 " + MaxMessageLength + " 个字符。", nameof(message));
        }

        return new FriendRequest(Guid.NewGuid(), requesterId, addresseeId, normalized, DateTime.UtcNow);
    }

    public void Accept(DateTime respondedAtUtc)
    {
        EnsurePending();
        Status = FriendRequestStatus.Accepted;
        RespondedAtUtc = ToUtc(respondedAtUtc);
    }

    public void Reject(DateTime respondedAtUtc)
    {
        EnsurePending();
        Status = FriendRequestStatus.Rejected;
        RespondedAtUtc = ToUtc(respondedAtUtc);
    }

    private void EnsurePending()
    {
        if (Status != FriendRequestStatus.Pending)
        {
            throw new InvalidOperationException("该好友申请已处理，不能重复操作。");
        }
    }

    private static DateTime ToUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}

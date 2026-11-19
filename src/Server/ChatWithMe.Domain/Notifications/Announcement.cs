namespace ChatWithMe.Domain.Notifications;

public sealed class Announcement
{
    private Announcement() { }

    public Guid Id { get; private set; }
    public Guid AdminId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static Announcement Create(Guid adminId, string title, string content, DateTime utcNow)
    {
        if (adminId == Guid.Empty) throw new ArgumentException("发送人不能为空。", nameof(adminId));
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 100)
            throw new ArgumentException("通知标题需为 1 至 100 个字符。", nameof(title));
        if (string.IsNullOrWhiteSpace(content) || content.Trim().Length > 10000)
            throw new ArgumentException("通知正文需为 1 至 10000 个字符。", nameof(content));

        return new Announcement { Id = Guid.NewGuid(), AdminId = adminId, Title = title.Trim(),
            Content = content.Trim(), CreatedAtUtc = utcNow };
    }
}

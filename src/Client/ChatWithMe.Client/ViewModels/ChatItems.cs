using CommunityToolkit.Mvvm.ComponentModel;

namespace ChatWithMe.Client.ViewModels;

public sealed record ConversationItem(Guid Id, string Title, string Subtitle, string Type);

/// <summary>一条消息。时间默认隐藏：滚动时短暂显示，鼠标悬停单条时显示。</summary>
public sealed partial class MessageItem : ObservableObject
{
    public MessageItem(Guid id, string senderName, string content, string time, bool isMine)
    {
        Id = id;
        SenderName = senderName;
        Content = content;
        Time = time;
        IsMine = isMine;
    }

    public Guid Id { get; }

    public string SenderName { get; }

    public string Content { get; }

    public string Time { get; }

    public bool IsMine { get; }

    [ObservableProperty]
    private bool _isTimeVisible;
}

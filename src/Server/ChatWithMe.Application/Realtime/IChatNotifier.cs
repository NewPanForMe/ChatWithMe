using ChatWithMe.Application.Messages;

namespace ChatWithMe.Application.Realtime;

/// <summary>消息实时推送抽象：Application 不直接依赖 SignalR。</summary>
public interface IChatNotifier
{
    Task MessageReceivedAsync(MessageDto message, CancellationToken cancellationToken);
}

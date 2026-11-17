using ChatWithMe.Application.Realtime;

namespace ChatWithMe.Api.Realtime;

public sealed class SignalROnlineSessionTerminator(IConnectionTracker connectionTracker) : IOnlineSessionTerminator
{
    public Task<int> TerminateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var contexts = connectionTracker.GetContexts(userId);

        foreach (var context in contexts)
        {
            try
            {
                context.Abort();
            }
            catch (Exception)
            {
                // 连接可能已自行断开，忽略。
            }
        }

        return Task.FromResult(contexts.Count);
    }
}

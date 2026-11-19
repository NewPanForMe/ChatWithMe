using ChatWithMe.Client.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatWithMe.Client.Services;

/// <summary>SignalR 实时连接：连到 /hubs/chat，加入会话组后接收 messageReceived 推送。</summary>
public sealed class ChatConnection(ApiClient apiClient, Session session) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private HubConnection? _connection;

    public event Action<MessageDto>? MessageReceived;
    public event Action<AnnouncementDto>? NotificationReceived;

    public event Action? Reconnected;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task EnsureStartedAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_connection is null)
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl(new Uri(apiClient.BaseAddress, "hubs/chat"), options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(apiClient.AccessToken);
                    })
                    .WithAutomaticReconnect()
                    .Build();

                _connection.On<MessageDto>("messageReceived", message => MessageReceived?.Invoke(message));
                _connection.On<AnnouncementDto>("notificationReceived", notification => NotificationReceived?.Invoke(notification));
                // 管理员踢人会主动断开连接；通过 HTTP 校验区分会话吊销与普通网络中断。
                _connection.Reconnecting += _ => CheckSessionAsync();
                _connection.Closed += _ => CheckSessionAsync();
                _connection.Reconnected += _ =>
                {
                    Reconnected?.Invoke();
                    return Task.CompletedTask;
                };
            }

            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task CheckSessionAsync()
    {
        if (!session.IsAuthenticated || apiClient.AccessToken is null)
        {
            return;
        }

        try
        {
            await apiClient.GetUserAsync(session.UserId).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // 鉴权拒绝由 ApiClient.SessionExpired 通知应用；网络故障保留自动重连。
        }
    }

    public async Task JoinConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        if (_connection is not { State: HubConnectionState.Connected })
        {
            return;
        }

        try
        {
            await _connection.InvokeAsync("JoinConversation", conversationId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // 不是成员或会话不存在时忽略，不影响其他会话。
        }
    }


    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}

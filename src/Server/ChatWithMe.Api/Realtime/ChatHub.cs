using ChatWithMe.Api.Extensions;
using ChatWithMe.Application.Realtime;
using ChatWithMe.Domain.Conversations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatWithMe.Api.Realtime;

/// <summary>
/// 实时聊天通道。客户端连接后调用 JoinConversation(conversationId) 加入会话组，
/// 服务端发送消息时向该组广播 messageReceived 事件。
/// </summary>
[Authorize(Policy = ChatWithMe.Api.Auth.AuthPolicies.ChatUser)]
public sealed class ChatHub(
    IOnlineUserRegistry onlineUsers,
    IConnectionTracker connectionTracker,
    IConversationRepository conversationRepository,
    ILogger<ChatHub> logger) : Hub
{
    public static string ConversationGroup(Guid conversationId) => "conversation:" + conversationId;
    public const string AnnouncementGroup = "chat-users:announcements";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.GetUserId();

        if (userId is null)
        {
            logger.LogWarning("未认证的实时连接被拒绝：{ConnectionId}", Context.ConnectionId);
            Context.Abort();
            return;
        }

        onlineUsers.Connect(userId.Value, Context.ConnectionId);
        connectionTracker.Add(userId.Value, Context.ConnectionId, Context);
        await Groups.AddToGroupAsync(Context.ConnectionId, AnnouncementGroup, Context.ConnectionAborted);

        logger.LogInformation(
            "实时连接已建立：用户 {UserId} 连接 {ConnectionId}，当前连接数 {Count}",
            userId.Value,
            Context.ConnectionId,
            onlineUsers.GetConnectionCount(userId.Value));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.GetUserId();

        if (userId is not null)
        {
            onlineUsers.Disconnect(userId.Value, Context.ConnectionId);
        }

        connectionTracker.Remove(Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = Context.User?.GetUserId() ?? throw new HubException("未认证的实时连接。");

        var conversation = await conversationRepository.GetByIdAsync(conversationId, Context.ConnectionAborted)
            ?? throw new HubException("会话不存在。");

        if (!conversation.IsMember(userId))
        {
            throw new HubException("不是该会话成员，不能订阅。");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId), Context.ConnectionAborted);
    }

    public Task LeaveConversation(Guid conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId), Context.ConnectionAborted);
}

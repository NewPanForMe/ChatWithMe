using ChatWithMe.Application.Messages;
using ChatWithMe.Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace ChatWithMe.Api.Realtime;

public sealed class SignalRChatNotifier(IHubContext<ChatHub> hubContext) : IChatNotifier
{
    public Task MessageReceivedAsync(MessageDto message, CancellationToken cancellationToken) =>
        hubContext.Clients
            .Group(ChatHub.ConversationGroup(message.ConversationId))
            .SendAsync("messageReceived", message, cancellationToken);
}

using ChatWithMe.Application.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace ChatWithMe.Api.Realtime;

public sealed class SignalRAnnouncementNotifier(IHubContext<ChatHub> hub) : IAnnouncementNotifier
{
    public Task BroadcastAsync(AnnouncementDto notification, CancellationToken cancellationToken) =>
        hub.Clients.Group(ChatHub.AnnouncementGroup).SendAsync("notificationReceived", notification, cancellationToken);
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ChatWithMe.Api.Auth;
using ChatWithMe.Api.Controllers;
using ChatWithMe.Api.Realtime;
using ChatWithMe.Application.Abstractions;
using ChatWithMe.Application.Auth;
using ChatWithMe.Application.Notifications;
using ChatWithMe.Application.Realtime;
using ChatWithMe.Domain.Conversations;
using ChatWithMe.Domain.Notifications;
using ChatWithMe.Infrastructure.Common;
using ChatWithMe.Tests.Fakes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatWithMe.Tests;

public sealed class AnnouncementTransportTests
{
    [Fact]
    public async Task AdminPublish_ReachesTwoClients_AndHistoryRequiresChatIdentity()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("test", _ => { });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.Admin, policy => policy.RequireClaim(AuthClaimNames.PrincipalType, AuthClaimNames.PrincipalAdmin).RequireClaim(AuthClaimNames.Role, "Admin"));
            options.AddPolicy(AuthPolicies.ChatUser, policy => policy.RequireClaim(AuthClaimNames.PrincipalType, AuthClaimNames.PrincipalUser));
        });
        builder.Services.AddControllers().AddApplicationPart(typeof(AnnouncementsController).Assembly);
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<IOnlineUserRegistry, InMemoryOnlineUserRegistry>();
        builder.Services.AddSingleton<IConnectionTracker, InMemoryConnectionTracker>();
        builder.Services.AddSingleton<IConversationRepository, FakeConversationRepository>();
        builder.Services.AddSingleton<IAnnouncementRepository, AnnouncementAppServiceTests.MemoryAnnouncements>();
        builder.Services.AddSingleton<IAnnouncementNotifier, SignalRAnnouncementNotifier>();
        builder.Services.AddSingleton<IClock, ChatWithMe.Infrastructure.Common.SystemClock>();
        builder.Services.AddScoped<AnnouncementAppService>();
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<ChatHub>("/hubs/chat");
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var http = new HttpClient { BaseAddress = new Uri(address) };

        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/notifications")).StatusCode);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "user");
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/api/admin/notifications", new { title = "标题", content = "内容" })).StatusCode);

        using var first = await ConnectAsync(address);
        using var second = await ConnectAsync(address);
        var online = app.Services.GetRequiredService<IOnlineUserRegistry>();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (online.GetOnlineUserIds().Count < 2) await Task.Delay(10, deadline.Token);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "admin");
        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/api/notifications")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsync("/hubs/chat/negotiate?negotiateVersion=1", null)).StatusCode);
        using var published = await http.PostAsJsonAsync("/api/admin/notifications", new { title = "全员通知", content = "完整正文\n第二行" });
        Assert.Equal(HttpStatusCode.Created, published.StatusCode);
        var result = await published.Content.ReadFromJsonAsync<PublishAnnouncementResult>();
        Assert.NotNull(result);
        Assert.True(result.RealtimeDelivered);
        var one = await ReadInvocationAsync(first, deadline.Token);
        var two = await ReadInvocationAsync(second, deadline.Token);
        Assert.Equal(result.Notification.Id.ToString(), one.GetProperty("id").GetString());
        Assert.Equal(result.Notification.Id.ToString(), two.GetProperty("id").GetString());
        Assert.Equal("完整正文\n第二行", one.GetProperty("content").GetString());

        // 未参与实时推送的客户端同样可登录后补拉，并按 id 读取详情。
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "user");
        using var history = await http.GetAsync("/api/notifications?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using var historyJson = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        Assert.Equal(1, historyJson.RootElement.GetProperty("total").GetInt32());
        var detail = await http.GetFromJsonAsync<AnnouncementDto>("/api/notifications/" + result.Notification.Id);
        Assert.Equal(result.Notification, detail);
        first.Dispose();
        second.Dispose();
        await app.StopAsync();
    }

    private static async Task<ClientWebSocket> ConnectAsync(string address)
    {
        var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("Authorization", "Bearer user");
        await socket.ConnectAsync(new Uri(address.Replace("http://", "ws://") + "/hubs/chat"), CancellationToken.None);
        await socket.SendAsync(Encoding.UTF8.GetBytes("{\"protocol\":\"json\",\"version\":1}\u001e"), WebSocketMessageType.Text, true, CancellationToken.None);
        var buffer = new byte[1024];
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token);
        return socket;
    }

    private static async Task<JsonElement> ReadInvocationAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var pending = string.Empty;
        while (true)
        {
            var received = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            Assert.NotEqual(WebSocketMessageType.Close, received.MessageType);
            pending += Encoding.UTF8.GetString(buffer, 0, received.Count);
            int separator;
            while ((separator = pending.IndexOf('\u001e')) >= 0)
            {
                var payload = pending[..separator];
                pending = pending[(separator + 1)..];
                using var json = JsonDocument.Parse(payload);
                if (json.RootElement.TryGetProperty("target", out var target) && target.GetString() == "notificationReceived")
                    return json.RootElement.GetProperty("arguments")[0].Clone();
            }
        }
    }

    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var token = Request.Headers.Authorization.ToString();
            if (token is not ("Bearer admin" or "Bearer user")) return Task.FromResult(AuthenticateResult.NoResult());
            var type = token == "Bearer admin" ? AuthClaimNames.PrincipalAdmin : AuthClaimNames.PrincipalUser;
            var claims = new[] { new Claim("sub", Guid.NewGuid().ToString()), new Claim(AuthClaimNames.PrincipalType, type),
                new Claim(AuthClaimNames.Role, token == "Bearer admin" ? "Admin" : "Member") };
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }
}

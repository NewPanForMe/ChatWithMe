using System.Net;
using System.Net.Http.Json;
using ChatWithMe.Client.Services;
using ChatWithMe.Client.Models;
using ChatWithMe.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ChatWithMe.Client.Tests;

public sealed class SessionExpirationTests
{
    [Fact]
    public async Task NewLoginScope_HasFreshSessionAndChatState()
    {
        await using var services = (ServiceProvider)AppServices.Build();
        await using var oldScope = services.CreateAsyncScope();
        var oldSession = oldScope.ServiceProvider.GetRequiredService<Session>();
        oldSession.SetUser(new UserDto(Guid.NewGuid(), "old@example.com", "旧账号", "2026/11/17 17:00:00"));
        var oldChat = oldScope.ServiceProvider.GetRequiredService<ChatPageViewModel>();
        oldChat.Draft = "旧会话草稿";

        await using var newScope = services.CreateAsyncScope();
        var newSession = newScope.ServiceProvider.GetRequiredService<Session>();
        var newChat = newScope.ServiceProvider.GetRequiredService<ChatPageViewModel>();

        Assert.False(newSession.IsAuthenticated);
        Assert.Empty(newChat.Draft);
        Assert.Empty(newChat.Messages);
        Assert.NotSame(oldChat, newChat);
        Assert.Same(newChat, newScope.ServiceProvider.GetRequiredService<ChatPageViewModel>());
        Assert.NotSame(oldScope.ServiceProvider.GetRequiredService<ChatConnection>(),
            newScope.ServiceProvider.GetRequiredService<ChatConnection>());
    }

    [Fact]
    public async Task RevokedSession_ClearsTokenAndNotifiesOnce()
    {
        using var api = CreateClient((path, _) => path == "/api/auth/login"
            ? LoginResponse("old-token")
            : new HttpResponseMessage(HttpStatusCode.Unauthorized));
        await api.LoginAsync("user@example.com", "password");
        var expired = 0;
        api.SessionExpired += () => expired++;

        await Assert.ThrowsAsync<ApiException>(() => api.GetFriendsAsync());
        await Assert.ThrowsAsync<ApiException>(() => api.GetFriendsAsync());

        Assert.Null(api.AccessToken);
        Assert.Equal(1, expired);
    }

    [Fact]
    public async Task ExpiredAccessToken_RefreshesWithoutEndingSession()
    {
        using var api = CreateClient((path, token) => path switch
        {
            "/api/auth/login" => LoginResponse("old-token"),
            "/api/auth/refresh" => LoginResponse("new-token"),
            _ when token == "new-token" => JsonResponse(Array.Empty<object>()),
            _ => new HttpResponseMessage(HttpStatusCode.Unauthorized),
        });
        await api.LoginAsync("user@example.com", "password");
        var expired = 0;
        api.SessionExpired += () => expired++;

        Assert.Empty(await api.GetFriendsAsync());
        Assert.Equal("new-token", api.AccessToken);
        Assert.Equal(0, expired);
    }

    [Fact]
    public async Task RefreshServiceUnavailable_DoesNotEndSession()
    {
        using var api = CreateClient((path, _) => path switch
        {
            "/api/auth/login" => LoginResponse("old-token"),
            "/api/auth/refresh" => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            _ => new HttpResponseMessage(HttpStatusCode.Unauthorized),
        });
        await api.LoginAsync("user@example.com", "password");
        var expired = 0;
        api.SessionExpired += () => expired++;

        var error = await Assert.ThrowsAsync<ApiException>(() => api.GetFriendsAsync());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, error.StatusCode);
        Assert.Equal("old-token", api.AccessToken);
        Assert.Equal(0, expired);
    }

    [Fact]
    public async Task UnauthorizedAfterRefresh_EndsSession()
    {
        using var api = CreateClient((path, _) => path switch
        {
            "/api/auth/login" => LoginResponse("old-token"),
            "/api/auth/refresh" => LoginResponse("new-token"),
            _ => new HttpResponseMessage(HttpStatusCode.Unauthorized),
        });
        await api.LoginAsync("user@example.com", "password");
        var expired = 0;
        api.SessionExpired += () => expired++;

        await Assert.ThrowsAsync<ApiException>(() => api.GetFriendsAsync());

        Assert.Null(api.AccessToken);
        Assert.Equal(1, expired);
    }

    private static ApiClient CreateClient(Func<string, string?, HttpResponseMessage> respond) =>
        new(new HttpClient(new ResponseHandler(respond)) { BaseAddress = new Uri("http://localhost/") });

    private static HttpResponseMessage LoginResponse(string token) => JsonResponse(new
    {
        accessToken = token,
        tokenType = "Bearer",
        accessTokenExpiresAtUtc = "2026/11/17 18:00:00",
        refreshTokenExpiresAtUtc = "2026/11/24 18:00:00",
        user = new { id = Guid.NewGuid(), email = "user@example.com", nickName = "用户", createdAtUtc = "2026/11/17 17:00:00" },
    });

    private static HttpResponseMessage JsonResponse(object body) => new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };

    private sealed class ResponseHandler(Func<string, string?, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request.RequestUri!.AbsolutePath, request.Headers.Authorization?.Parameter));
    }
}

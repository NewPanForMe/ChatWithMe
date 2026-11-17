using ChatWithMe.Api.Realtime;
using ChatWithMe.Application.Admin;
using ChatWithMe.Application.Common;
using ChatWithMe.Domain.Auth;
using ChatWithMe.Domain.Users;
using ChatWithMe.Infrastructure.Common;
using ChatWithMe.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatWithMe.Tests;

public sealed class AdminUserAppServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeOnlineUserRegistry _online = new();
    private readonly FakeSessionTerminator _terminator = new();
    private readonly AdminUserAppService _sut;

    public AdminUserAppServiceTests()
    {
        _sut = new AdminUserAppService(
            _users,
            _refreshTokens,
            _online,
            _terminator,
            new SystemClock(),
            NullLogger<AdminUserAppService>.Instance);
    }

    [Fact]
    public async Task Kick_IncrementsSessionVersionAndRevokesRefreshTokens()
    {
        var user = _users.Add("kick1@example.com");
        var token = RefreshToken.Issue(user.Id, "hash-1", DateTime.UtcNow.AddDays(1));
        await _refreshTokens.AddAsync(token, CancellationToken.None);

        var result = await _sut.KickAsync(user.Id, CancellationToken.None);

        Assert.Equal(1, result.SessionVersion);
        Assert.Equal(1, result.RevokedRefreshTokens);
        Assert.Equal(1, user.SessionVersion);
        Assert.False(token.IsActive(DateTime.UtcNow));
        Assert.Contains(user.Id, _terminator.Terminated);
    }

    [Fact]
    public async Task Kick_UnknownUser_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.KickAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Kick_Twice_KeepsIncrementing()
    {
        var user = _users.Add("kick2@example.com");

        await _sut.KickAsync(user.Id, CancellationToken.None);
        var second = await _sut.KickAsync(user.Id, CancellationToken.None);

        Assert.Equal(2, second.SessionVersion);
    }

    [Fact]
    public async Task ListOnline_ReturnsOnlyConnectedUsers()
    {
        var online = _users.Add("online@example.com");
        var offline = _users.Add("offline@example.com");
        _online.Connect(online.Id, "conn-1");
        _online.Connect(online.Id, "conn-2");

        var result = await _sut.ListOnlineAsync(CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(online.Id, item.Id);
        Assert.Equal(2, item.Connections);
        Assert.DoesNotContain(result, user => user.Id == offline.Id);
    }

    [Fact]
    public async Task ListOnline_EmptyWhenNobodyConnected()
    {
        _users.Add("nobody@example.com");

        Assert.Empty(await _sut.ListOnlineAsync(CancellationToken.None));
    }

    [Fact]
    public void InMemoryOnlineUserRegistry_CountsMultipleConnections()
    {
        var registry = new InMemoryOnlineUserRegistry();
        var userId = Guid.NewGuid();

        registry.Connect(userId, "a");
        registry.Connect(userId, "b");

        Assert.True(registry.IsOnline(userId));
        Assert.Equal(2, registry.GetConnectionCount(userId));

        registry.Disconnect(userId, "a");
        Assert.Equal(1, registry.GetConnectionCount(userId));

        registry.Disconnect(userId, "b");
        Assert.False(registry.IsOnline(userId));
        Assert.Empty(registry.GetOnlineUserIds());
    }
}

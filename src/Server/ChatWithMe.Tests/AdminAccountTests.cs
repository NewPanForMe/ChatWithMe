using ChatWithMe.Application.Admin;
using ChatWithMe.Application.Common;
using ChatWithMe.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatWithMe.Tests;

public sealed class AdminAccountTests
{
    private readonly FakeAdminUserRepository _admins = new();
    private readonly AdminAccountAppService _sut;

    public AdminAccountTests()
    {
        _sut = new AdminAccountAppService(_admins, NullLogger<AdminAccountAppService>.Instance);
    }

    [Fact]
    public async Task Create_WithShortPassword_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.CreateAsync(new CreateAdminAccountRequest("new@example.com", "新管理员", "123"), CancellationToken.None));
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ThrowsConflict()
    {
        _admins.Add("dup@example.com");

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CreateAsync(new CreateAdminAccountRequest("dup@example.com", "重复", "Password@1"), CancellationToken.None));
    }

    [Fact]
    public async Task Create_ReturnsAccount()
    {
        var created = await _sut.CreateAsync(
            new CreateAdminAccountRequest("ok@example.com", "运营", "Password@1"),
            CancellationToken.None);

        Assert.Equal("ok@example.com", created.Email);
        Assert.True(created.IsActive);
        Assert.Single(await _sut.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Delete_Self_IsRefused()
    {
        var admin = _admins.Add("self@example.com");

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.DeleteAsync(admin.Id, admin.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_Other_RemovesAccount()
    {
        var me = _admins.Add("me@example.com");
        var other = _admins.Add("other@example.com");

        await _sut.DeleteAsync(other.Id, me.Id, CancellationToken.None);

        Assert.Single(await _sut.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Delete_Unknown_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.DeleteAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
    }
}

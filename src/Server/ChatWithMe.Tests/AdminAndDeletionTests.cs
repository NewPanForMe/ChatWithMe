using ChatWithMe.Application.Common;
using ChatWithMe.Application.Users;
using ChatWithMe.Domain.Admin;
using ChatWithMe.Infrastructure.Common;
using ChatWithMe.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatWithMe.Tests;

public sealed class AdminAndDeletionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void AdminUser_InvalidEmail_Throws(string email)
    {
        Assert.Throws<ArgumentException>(() => AdminUser.Create(email, "管理员", "hash"));
    }

    [Fact]
    public void AdminUser_InvalidateSessions_IncrementsVersion()
    {
        var admin = AdminUser.Create("admin@example.com", "管理员", "hash");

        admin.InvalidateSessions();

        Assert.Equal(1, admin.SessionVersion);
        Assert.True(admin.IsActive);
    }

    [Fact]
    public async Task DeleteUser_UnknownId_ThrowsNotFound()
    {
        var users = new FakeUserRepository();
        var service = new UserAppService(users, NullLogger<UserAppService>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteUser_RemovesUser()
    {
        var users = new FakeUserRepository();
        var user = users.Add("delete-me@example.com");
        var service = new UserAppService(users, NullLogger<UserAppService>.Instance);

        await service.DeleteAsync(user.Id, CancellationToken.None);

        Assert.Null(await users.GetByIdAsync(user.Id, CancellationToken.None));
    }
}

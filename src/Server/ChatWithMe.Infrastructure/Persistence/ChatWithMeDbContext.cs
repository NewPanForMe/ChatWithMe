using ChatWithMe.Domain.Auth;
using ChatWithMe.Domain.Files;
using ChatWithMe.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ChatWithMe.Infrastructure.Persistence;

public sealed class ChatWithMeDbContext(DbContextOptions<ChatWithMeDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<FileAsset> FileAssets => Set<FileAsset>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatWithMeDbContext).Assembly);
}

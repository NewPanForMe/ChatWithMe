using ChatWithMe.Domain.Admin;
using ChatWithMe.Domain.Auth;
using ChatWithMe.Domain.Conversations;
using ChatWithMe.Domain.Files;
using ChatWithMe.Domain.Friendships;
using ChatWithMe.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ChatWithMe.Infrastructure.Persistence;

public sealed class ChatWithMeDbContext(DbContextOptions<ChatWithMeDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    public DbSet<FileAsset> FileAssets => Set<FileAsset>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();

    public DbSet<Friendship> Friendships => Set<Friendship>();

    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();

    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatWithMeDbContext).Assembly);
}

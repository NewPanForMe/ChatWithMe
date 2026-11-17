using ChatWithMe.Domain.Friendships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatWithMe.Infrastructure.Persistence.Configurations;

public sealed class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.ToTable("friendships");

        builder.HasKey(friendship => new { friendship.UserId, friendship.FriendId });

        builder.Property(friendship => friendship.UserId)
            .HasColumnName("user_id");

        builder.Property(friendship => friendship.FriendId)
            .HasColumnName("friend_id");

        builder.Property(friendship => friendship.Remark)
            .HasColumnName("remark")
            .HasMaxLength(Friendship.MaxRemarkLength);

        builder.Property(friendship => friendship.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(friendship => friendship.FriendId)
            .HasDatabaseName("ix_friendships_friend_id");
    }
}

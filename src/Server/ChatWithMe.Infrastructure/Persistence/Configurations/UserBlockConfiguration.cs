using ChatWithMe.Domain.Friendships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatWithMe.Infrastructure.Persistence.Configurations;

public sealed class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.ToTable("user_blocks");

        builder.HasKey(block => new { block.BlockerId, block.BlockedId });

        builder.Property(block => block.BlockerId)
            .HasColumnName("blocker_id");

        builder.Property(block => block.BlockedId)
            .HasColumnName("blocked_id");

        builder.Property(block => block.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(block => block.BlockedId)
            .HasDatabaseName("ix_user_blocks_blocked_id");
    }
}

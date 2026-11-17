using ChatWithMe.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatWithMe.Infrastructure.Persistence.Configurations;

public sealed class ConversationMemberConfiguration : IEntityTypeConfiguration<ConversationMember>
{
    public void Configure(EntityTypeBuilder<ConversationMember> builder)
    {
        builder.ToTable("conversation_members");

        builder.HasKey(member => new { member.ConversationId, member.UserId });

        builder.Property(member => member.ConversationId)
            .HasColumnName("conversation_id");

        builder.Property(member => member.UserId)
            .HasColumnName("user_id");

        builder.Property(member => member.Role)
            .HasColumnName("role")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(member => member.JoinedAtUtc)
            .HasColumnName("joined_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(member => member.LastReadMessageId)
            .HasColumnName("last_read_message_id")
            .HasMaxLength(64);

        builder.Property(member => member.LastReadAtUtc)
            .HasColumnName("last_read_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(member => member.MutedUntilUtc)
            .HasColumnName("muted_until_utc")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(member => member.UserId)
            .HasDatabaseName("ix_conversation_members_user_id");
    }
}

using ChatWithMe.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatWithMe.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(conversation => conversation.Id);

        builder.Property(conversation => conversation.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(conversation => conversation.Type)
            .HasColumnName("type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(conversation => conversation.Title)
            .HasColumnName("title")
            .HasMaxLength(Conversation.MaxTitleLength);

        builder.Property(conversation => conversation.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(conversation => conversation.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(conversation => conversation.CreatedByUserId)
            .HasDatabaseName("ix_conversations_created_by_user_id");

        builder.HasMany(conversation => conversation.Members)
            .WithOne()
            .HasForeignKey(member => member.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(conversation => conversation.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

using ChatWithMe.Domain.Friendships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatWithMe.Infrastructure.Persistence.Configurations;

public sealed class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        builder.ToTable("friend_requests");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(request => request.RequesterId)
            .HasColumnName("requester_id")
            .IsRequired();

        builder.Property(request => request.AddresseeId)
            .HasColumnName("addressee_id")
            .IsRequired();

        builder.Property(request => request.Message)
            .HasColumnName("message")
            .HasMaxLength(FriendRequest.MaxMessageLength);

        builder.Property(request => request.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(request => request.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(request => request.RespondedAtUtc)
            .HasColumnName("responded_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(request => new { request.AddresseeId, request.Status })
            .HasDatabaseName("ix_friend_requests_addressee_status");

        builder.HasIndex(request => new { request.RequesterId, request.Status })
            .HasDatabaseName("ix_friend_requests_requester_status");

        builder.Ignore(request => request.DomainEvents);
    }
}

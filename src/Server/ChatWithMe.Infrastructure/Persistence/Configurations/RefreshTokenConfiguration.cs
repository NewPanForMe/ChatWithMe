using ChatWithMe.Domain.Auth;
using ChatWithMe.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatWithMe.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    private const int TokenHashLength = 64;

    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(refreshToken => refreshToken.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(refreshToken => refreshToken.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(TokenHashLength)
            .IsRequired();

        builder.HasIndex(refreshToken => refreshToken.TokenHash)
            .IsUnique()
            .HasDatabaseName("ix_refresh_tokens_token_hash");

        builder.Property(refreshToken => refreshToken.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(refreshToken => refreshToken.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(refreshToken => refreshToken.RevokedAtUtc)
            .HasColumnName("revoked_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(refreshToken => refreshToken.ReplacedByTokenHash)
            .HasColumnName("replaced_by_token_hash")
            .HasMaxLength(TokenHashLength);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_refresh_tokens_users_user_id");

        builder.HasIndex(refreshToken => refreshToken.UserId)
            .HasDatabaseName("ix_refresh_tokens_user_id");
    }
}

using ChatWithMe.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatWithMe.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.OwnsOne(user => user.Email, email =>
        {
            email.Property(value => value.Value)
                .HasColumnName("email")
                .HasMaxLength(256)
                .IsRequired();

            email.HasIndex(value => value.Value).IsUnique();
        });

        builder.Property(user => user.NickName)
            .HasColumnName("nick_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(400)
            .IsRequired();

        builder.Property(user => user.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(user => user.CreatedAtUtc)
            .HasDatabaseName("ix_users_created_at_utc");

        builder.Ignore(user => user.DomainEvents);
    }
}

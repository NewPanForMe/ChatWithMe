using ChatWithMe.Domain.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatWithMe.Infrastructure.Persistence.Configurations;

public sealed class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("admin_users");

        builder.HasKey(admin => admin.Id);

        builder.Property(admin => admin.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(admin => admin.Email)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(admin => admin.Email)
            .IsUnique()
            .HasDatabaseName("ux_admin_users_email");

        builder.Property(admin => admin.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(admin => admin.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(400)
            .IsRequired();

        builder.Property(admin => admin.SessionVersion)
            .HasColumnName("session_version")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(admin => admin.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(admin => admin.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Ignore(admin => admin.DomainEvents);
    }
}

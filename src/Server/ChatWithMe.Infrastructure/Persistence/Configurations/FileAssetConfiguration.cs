using ChatWithMe.Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatWithMe.Infrastructure.Persistence.Configurations;

public sealed class FileAssetConfiguration : IEntityTypeConfiguration<FileAsset>
{
    public void Configure(EntityTypeBuilder<FileAsset> builder)
    {
        builder.ToTable("file_assets");

        builder.HasKey(fileAsset => fileAsset.Id);

        builder.Property(fileAsset => fileAsset.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(fileAsset => fileAsset.OriginalName)
            .HasColumnName("original_name")
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(fileAsset => fileAsset.StoredName)
            .HasColumnName("stored_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(fileAsset => fileAsset.StoredName)
            .IsUnique()
            .HasDatabaseName("ix_file_assets_stored_name");

        builder.Property(fileAsset => fileAsset.RelativePath)
            .HasColumnName("relative_path")
            .HasMaxLength(400)
            .IsRequired();

        builder.Property(fileAsset => fileAsset.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(fileAsset => fileAsset.SizeInBytes)
            .HasColumnName("size_in_bytes")
            .IsRequired();

        builder.Property(fileAsset => fileAsset.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(fileAsset => fileAsset.CreatedAtUtc)
            .HasDatabaseName("ix_file_assets_created_at_utc");
    }
}

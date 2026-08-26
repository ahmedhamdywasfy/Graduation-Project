using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="HorseImage"/>. Originally added Sprint 1 (URL + IsPrimary
/// only); extended Sprint 2 §5–§7 with full upload/gallery metadata and the
/// Cloudinary <c>StorageId</c> needed to delete the remote asset.
/// </summary>
public class HorseImageConfiguration : IEntityTypeConfiguration<HorseImage>
{
    public void Configure(EntityTypeBuilder<HorseImage> builder)
    {
        builder.ToTable("HorseImages");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ImageUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(i => i.StorageId)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(i => i.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.ContentHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(i => i.UploadedAtUtc)
            .IsRequired();

        builder.HasIndex(i => i.HorseId);

        // Backs the Sprint 2 §6 "Duplicate Images" check — one query per upload
        // instead of loading the whole gallery to compare hashes in memory.
        builder.HasIndex(i => new { i.HorseId, i.ContentHash })
            .IsUnique();
    }
}

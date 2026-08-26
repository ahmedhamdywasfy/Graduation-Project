using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Configurations;

public class HorseImageConfiguration : IEntityTypeConfiguration<HorseImage>
{
    public void Configure(EntityTypeBuilder<HorseImage> builder)
    {
        builder.ToTable("HorseImages");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ImageUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(i => i.UploadedAtUtc)
            .IsRequired();

        builder.HasIndex(i => i.HorseId);
    }
}

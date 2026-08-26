using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Configurations;

public class HorseStatusConfiguration : IEntityTypeConfiguration<HorseStatus>
{
    public void Configure(EntityTypeBuilder<HorseStatus> builder)
    {
        builder.ToTable("HorseStatuses");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Name).IsUnique();
    }
}

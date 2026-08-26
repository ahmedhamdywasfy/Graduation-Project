using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Configurations;

public class OwnershipHistoryConfiguration : IEntityTypeConfiguration<OwnershipHistory>
{
    public void Configure(EntityTypeBuilder<OwnershipHistory> builder)
    {
        builder.ToTable("OwnershipHistories");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Notes)
            .HasMaxLength(1000);

        builder.Property(o => o.ChangedAtUtc)
            .IsRequired();

        builder.HasIndex(o => new { o.HorseId, o.ChangedAtUtc });

        // PreviousOwner is nullable (first record has none) and must NOT cascade
        // through the same path as NewOwner, or SQL Server will reject multiple
        // cascade paths from OwnershipHistories to Users.
        builder.HasOne(o => o.PreviousOwner)
            .WithMany()
            .HasForeignKey(o => o.PreviousOwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.NewOwner)
            .WithMany()
            .HasForeignKey(o => o.NewOwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

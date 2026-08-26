using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="OwnershipHistory"/>. Originally added Sprint 1; extended
/// Sprint 2 §1 with <c>SaleDate</c> and soft-delete support (the entity now
/// derives from <c>SoftDeletableAuditableEntity</c> instead of
/// <c>BaseEntity</c> — see the entity's doc comment for why the original
/// <c>ChangedAtUtc</c> column name was kept rather than renamed).
/// </summary>
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

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.HasIndex(o => new { o.HorseId, o.ChangedAtUtc });
        builder.HasIndex(o => new { o.HorseId, o.SaleDate });

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

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}

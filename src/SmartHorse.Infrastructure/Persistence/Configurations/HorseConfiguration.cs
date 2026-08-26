using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Horse"/> (Person 2 Sprint 1 §1–§4). Note the global soft-delete
/// query filter: every normal query (Get/Search/GetAll) automatically excludes
/// deleted horses without callers needing to remember a <c>Where(!IsDeleted)</c>
/// clause; Restore explicitly bypasses it via <c>IgnoreQueryFilters()</c> in
/// HorseRepository.GetDeletedByIdAsync.
/// </summary>
public class HorseConfiguration : IEntityTypeConfiguration<Horse>
{
    public void Configure(EntityTypeBuilder<Horse> builder)
    {
        builder.ToTable("Horses");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(h => h.Weight)
            .IsRequired()
            .HasColumnType("decimal(7,2)");

        builder.Property(h => h.Height)
            .IsRequired()
            .HasColumnType("decimal(6,2)");

        builder.Property(h => h.BirthDate)
            .IsRequired();

        builder.Property(h => h.Description)
            .HasMaxLength(2000);

        builder.Property(h => h.MicrochipNumber)
            .HasMaxLength(50);

        builder.Property(h => h.RegistrationNumber)
            .HasMaxLength(50);

        // Unique but nullable — a filtered unique index so multiple horses can
        // each have a NULL microchip/registration without violating uniqueness
        // (Person 2 Sprint 1 §7 — "Duplicate Registration Number" / "Duplicate
        // Microchip Number" only applies once a value is actually supplied).
        builder.HasIndex(h => h.MicrochipNumber)
            .IsUnique()
            .HasFilter("[MicrochipNumber] IS NOT NULL");

        builder.HasIndex(h => h.RegistrationNumber)
            .IsUnique()
            .HasFilter("[RegistrationNumber] IS NOT NULL");

        builder.HasIndex(h => h.Name);
        builder.HasIndex(h => h.CreatedAt);
        builder.HasIndex(h => h.StatusId);
        builder.HasIndex(h => h.CurrentOwnerId);

        builder.HasOne(h => h.Breed)
            .WithMany()
            .HasForeignKey(h => h.BreedId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Color)
            .WithMany()
            .HasForeignKey(h => h.ColorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Gender)
            .WithMany()
            .HasForeignKey(h => h.GenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Status)
            .WithMany()
            .HasForeignKey(h => h.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.CurrentOwner)
            .WithMany()
            .HasForeignKey(h => h.CurrentOwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing lineage FKs (Sprint 2 §3). Both Restrict — SQL Server
        // disallows multiple cascade paths from Horses back to itself through two
        // different FK columns, and cascading a horse's soft-deletable audit-only
        // parent link isn't meaningful anyway (soft delete is the removal path).
        builder.HasOne(h => h.Father)
            .WithMany()
            .HasForeignKey(h => h.FatherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Mother)
            .WithMany()
            .HasForeignKey(h => h.MotherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => h.FatherId);
        builder.HasIndex(h => h.MotherId);

        builder.HasMany(h => h.Images)
            .WithOne(i => i.Horse)
            .HasForeignKey(i => i.HorseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(h => h.OwnershipHistory)
            .WithOne(o => o.Horse)
            .HasForeignKey(o => o.HorseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Horse.Images))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Horse.OwnershipHistory))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(h => !h.IsDeleted);
    }
}

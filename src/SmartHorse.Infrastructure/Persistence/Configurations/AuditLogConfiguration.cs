using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="AuditLog"/> (Sprint 2 §6 / v0.1 §13 AuditLogs).</summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.IpAddress)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(a => a.UserAgent)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Details)
            .HasMaxLength(1000);

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();

        // AuditLogs is append-only and high-write (v0.2 §9.5) — index on the
        // (UserId, CreatedAtUtc) pair covers the two most common queries: "history
        // for this user" and "recent events across all users".
        builder.HasIndex(a => new { a.UserId, a.CreatedAtUtc });
        builder.HasIndex(a => a.Action);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

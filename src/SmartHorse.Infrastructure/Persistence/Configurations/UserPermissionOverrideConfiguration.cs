using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Configurations;

public class UserPermissionOverrideConfiguration : IEntityTypeConfiguration<UserPermissionOverride>
{
    public void Configure(EntityTypeBuilder<UserPermissionOverride> builder)
    {
        builder.ToTable("UserPermissionOverrides");

        builder.HasKey(upo => new { upo.UserId, upo.PermissionId });

        builder.HasOne(upo => upo.User)
            .WithMany(u => u.PermissionOverrides)
            .HasForeignKey(upo => upo.UserId);

        builder.HasOne(upo => upo.Permission)
            .WithMany()
            .HasForeignKey(upo => upo.PermissionId);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.Access;

namespace StampService.Infrastructure.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .IsRequired()
            .HasColumnName("id");

        builder.Property(r => r.SystemName)
            .IsRequired()
            .HasMaxLength(Constants.MAX_ROLE_SYSTEM_NAME_LENGTH)
            .HasColumnName("system_name");

        builder.Property(r => r.DisplayName)
            .IsRequired()
            .HasMaxLength(Constants.MAX_ROLE_DISPLAY_NAME_LENGTH)
            .HasColumnName("display_name");

        builder.HasIndex(r => r.SystemName)
            .IsUnique()
            .HasDatabaseName("ix_roles_system_name");
    }
}

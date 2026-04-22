using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.Loyalty;

namespace StampService.Infrastructure.Configurations;

public class LoyaltyMetricDefinitionConfiguration : IEntityTypeConfiguration<LoyaltyMetricDefinition>
{
    public void Configure(EntityTypeBuilder<LoyaltyMetricDefinition> builder)
    {
        builder.ToTable("loyalty_metric_definitions");

        builder.HasKey(lmd => lmd.Id);

        builder.Property(lmd => lmd.Id)
            .IsRequired()
            .HasColumnName("id");

        builder.Property(lmd => lmd.BrandId)
            .IsRequired()
            .HasColumnName("brand_id");

        builder.Property(lmd => lmd.Code)
            .IsRequired()
            .HasMaxLength(Constants.MAX_METRIC_CODE_LENGTH)
            .HasColumnName("code");

        builder.Property(lmd => lmd.Name)
            .IsRequired()
            .HasMaxLength(Constants.MAX_METRIC_NAME_LENGTH)
            .HasColumnName("name");

        builder.Property(lmd => lmd.IsActive)
            .IsRequired()
            .HasColumnName("is_active");

        builder.HasIndex(lmd => new { lmd.BrandId, lmd.Code })
            .IsUnique()
            .HasDatabaseName("ix_loyalty_metric_definitions_brand_id_code");

        builder.HasOne(lmd => lmd.Brand)
            .WithMany()
            .HasForeignKey(lmd => lmd.BrandId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(lmd => lmd.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(lmd => lmd.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(lmd => lmd.DeletedAt)
            .HasColumnName("deleted_at");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.Loyalty;
using BrandEntity = StampService.Domain.Brand.Brand;
using UserEntity = StampService.Domain.User.User;

namespace StampService.Infrastructure.Configurations;

public class MetricBalanceConfiguration : IEntityTypeConfiguration<MetricBalance>
{
    public void Configure(EntityTypeBuilder<MetricBalance> builder)
    {
        builder.ToTable("metric_balances");

        builder.HasKey(mb => mb.Id);

        builder.Property(mb => mb.Id)
            .IsRequired()
            .HasColumnName("id");

        builder.Property(mb => mb.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(mb => mb.BrandId)
            .IsRequired()
            .HasColumnName("brand_id");

        builder.Property(mb => mb.MetricDefinitionId)
            .IsRequired()
            .HasColumnName("metric_definition_id");

        builder.Property(mb => mb.Value)
            .IsRequired()
            .HasColumnName("value");

        builder.HasIndex(mb => new { mb.UserId, mb.BrandId, mb.MetricDefinitionId })
            .IsUnique()
            .HasDatabaseName("ix_metric_balances_user_id_brand_id_metric_definition_id");

        builder.HasOne(mb => mb.User)
            .WithMany()
            .HasForeignKey(mb => mb.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mb => mb.Brand)
            .WithMany()
            .HasForeignKey(mb => mb.BrandId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mb => mb.MetricDefinition)
            .WithMany()
            .HasForeignKey(mb => mb.MetricDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(mb => mb.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(mb => mb.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(mb => mb.DeletedAt)
            .HasColumnName("deleted_at");
    }
}

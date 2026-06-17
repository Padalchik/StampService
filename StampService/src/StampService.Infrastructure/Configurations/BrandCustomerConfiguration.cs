using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.Brand;

namespace StampService.Infrastructure.Configurations;

public class BrandCustomerConfiguration : IEntityTypeConfiguration<BrandCustomer>
{
    public void Configure(EntityTypeBuilder<BrandCustomer> builder)
    {
        builder.ToTable("brand_customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Id)
            .IsRequired()
            .HasColumnName("id");

        builder.Property(customer => customer.BrandId)
            .IsRequired()
            .HasColumnName("brand_id");

        builder.Property(customer => customer.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(customer => customer.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.HasIndex(customer => new { customer.BrandId, customer.UserId })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_brand_customers_brand_id_user_id");

        builder.HasOne(customer => customer.Brand)
            .WithMany()
            .HasForeignKey(customer => customer.BrandId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(customer => customer.User)
            .WithMany()
            .HasForeignKey(customer => customer.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(customer => customer.CreatedByUser)
            .WithMany()
            .HasForeignKey(customer => customer.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(customer => customer.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(customer => customer.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(customer => customer.DeletedAt)
            .HasColumnName("deleted_at");
    }
}

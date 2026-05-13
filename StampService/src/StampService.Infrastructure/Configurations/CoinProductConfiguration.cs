using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.Coins;

namespace StampService.Infrastructure.Configurations;

public class CoinProductConfiguration : IEntityTypeConfiguration<CoinProduct>
{
    public void Configure(EntityTypeBuilder<CoinProduct> builder)
    {
        builder.ToTable("coin_products", table =>
        {
            table.HasCheckConstraint("ck_coin_products_price_positive", "price > 0");
        });

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id)
            .IsRequired()
            .HasColumnName("id");

        builder.Property(product => product.BrandId)
            .IsRequired()
            .HasColumnName("brand_id");

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(Constants.MAX_COIN_PRODUCT_NAME_LENGTH)
            .HasColumnName("name");

        builder.Property(product => product.Price)
            .IsRequired()
            .HasColumnName("price");

        builder.Property(product => product.IsActive)
            .IsRequired()
            .HasColumnName("is_active");

        builder.HasIndex(product => product.BrandId)
            .HasDatabaseName("ix_coin_products_brand_id");

        builder.HasOne(product => product.Brand)
            .WithMany()
            .HasForeignKey(product => product.BrandId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(product => product.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(product => product.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(product => product.DeletedAt)
            .HasColumnName("deleted_at");
    }
}

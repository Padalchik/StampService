using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.Coins;

namespace StampService.Infrastructure.Configurations;

public class CoinWalletConfiguration : IEntityTypeConfiguration<CoinWallet>
{
    public void Configure(EntityTypeBuilder<CoinWallet> builder)
    {
        builder.ToTable("coin_wallets", table =>
        {
            table.HasCheckConstraint("ck_coin_wallets_value_non_negative", "value >= 0");
        });

        builder.HasKey(wallet => wallet.Id);

        builder.Property(wallet => wallet.Id)
            .IsRequired()
            .HasColumnName("id");

        builder.Property(wallet => wallet.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(wallet => wallet.BrandId)
            .IsRequired()
            .HasColumnName("brand_id");

        builder.Property(wallet => wallet.Value)
            .IsRequired()
            .HasColumnName("value");

        builder.HasIndex(wallet => new { wallet.UserId, wallet.BrandId })
            .IsUnique()
            .HasDatabaseName("ix_coin_wallets_user_id_brand_id");

        builder.HasOne(wallet => wallet.User)
            .WithMany()
            .HasForeignKey(wallet => wallet.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wallet => wallet.Brand)
            .WithMany()
            .HasForeignKey(wallet => wallet.BrandId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(wallet => wallet.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(wallet => wallet.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(wallet => wallet.DeletedAt)
            .HasColumnName("deleted_at");
    }
}

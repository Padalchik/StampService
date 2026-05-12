using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.Coins;

namespace StampService.Infrastructure.Configurations;

public class CoinTransactionConfiguration : IEntityTypeConfiguration<CoinTransaction>
{
    public void Configure(EntityTypeBuilder<CoinTransaction> builder)
    {
        builder.ToTable("coin_transactions", table =>
        {
            table.HasCheckConstraint("ck_coin_transactions_amount_positive", "amount > 0");
            table.HasCheckConstraint("ck_coin_transactions_transaction_type", "transaction_type IN (1, 2)");
        });

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .IsRequired()
            .HasColumnName("id");

        builder.Property(transaction => transaction.CoinWalletId)
            .IsRequired()
            .HasColumnName("coin_wallet_id");

        builder.Property(transaction => transaction.Type)
            .IsRequired()
            .HasColumnName("transaction_type");

        builder.Property(transaction => transaction.Amount)
            .IsRequired()
            .HasColumnName("amount");

        builder.Property(transaction => transaction.Comment)
            .IsRequired()
            .HasMaxLength(Constants.MAX_COIN_TRANSACTION_COMMENT_LENGTH)
            .HasColumnName("comment");

        builder.Property(transaction => transaction.ActorUserId)
            .IsRequired()
            .HasColumnName("actor_user_id");

        builder.HasOne(transaction => transaction.CoinWallet)
            .WithMany()
            .HasForeignKey(transaction => transaction.CoinWalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(transaction => transaction.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(transaction => transaction.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(transaction => transaction.DeletedAt)
            .HasColumnName("deleted_at");
    }
}

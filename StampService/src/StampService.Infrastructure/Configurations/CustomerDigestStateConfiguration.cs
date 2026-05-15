using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.User;

namespace StampService.Infrastructure.Configurations;

public class CustomerDigestStateConfiguration : IEntityTypeConfiguration<CustomerDigestState>
{
    public void Configure(EntityTypeBuilder<CustomerDigestState> builder)
    {
        builder.ToTable("customer_digest_states");

        builder.HasQueryFilter(state => state.User.DeletedAt == null);

        builder.HasKey(state => state.UserId);

        builder.Property(state => state.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(state => state.LastDigestSentAtUtc)
            .HasColumnName("last_digest_sent_at_utc");

        builder.Property(state => state.LastWalletOpenedAtUtc)
            .HasColumnName("last_wallet_opened_at_utc");

        builder.HasOne(state => state.User)
            .WithOne()
            .HasForeignKey<CustomerDigestState>(state => state.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(state => state.LastDigestSentAtUtc)
            .HasDatabaseName("ix_customer_digest_states_last_digest_sent_at_utc");

        builder.HasIndex(state => state.LastWalletOpenedAtUtc)
            .HasDatabaseName("ix_customer_digest_states_last_wallet_opened_at_utc");
    }
}

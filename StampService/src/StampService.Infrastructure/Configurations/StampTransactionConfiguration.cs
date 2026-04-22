using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.Loyalty;

namespace StampService.Infrastructure.Configurations;

public class StampTransactionConfiguration : IEntityTypeConfiguration<StampTransaction>
{
    public void Configure(EntityTypeBuilder<StampTransaction> builder)
    {
        builder.ToTable("stamp_transactions");

        builder.HasKey(st => st.Id);

        builder.Property(st => st.Id)
            .IsRequired()
            .HasColumnName("id");

        builder.Property(st => st.MetricBalanceId)
            .IsRequired()
            .HasColumnName("metric_balance_id");

        builder.Property(st => st.MetricDefinitionId)
            .IsRequired()
            .HasColumnName("metric_definition_id");

        builder.Property(st => st.Amount)
            .IsRequired()
            .HasColumnName("amount");

        builder.Property(st => st.Comment)
            .IsRequired()
            .HasMaxLength(Constants.MAX_TRANSACTION_COMMENT_LENGTH)
            .HasColumnName("comment");

        builder.HasIndex(st => st.MetricDefinitionId)
            .HasDatabaseName("ix_stamp_transactions_metric_definition_id");

        builder.HasOne(st => st.MetricBalance)
            .WithMany()
            .HasForeignKey(st => st.MetricBalanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(st => st.MetricDefinition)
            .WithMany()
            .HasForeignKey(st => st.MetricDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(st => st.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(st => st.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(st => st.DeletedAt)
            .HasColumnName("deleted_at");
    }
}

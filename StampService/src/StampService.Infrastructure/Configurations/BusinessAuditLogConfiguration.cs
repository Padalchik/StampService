using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.Audit;

namespace StampService.Infrastructure.Configurations;

public class BusinessAuditLogConfiguration : IEntityTypeConfiguration<BusinessAuditLog>
{
    public void Configure(EntityTypeBuilder<BusinessAuditLog> builder)
    {
        builder.ToTable("business_audit_logs", table =>
        {
            table.HasCheckConstraint(
                "ck_business_audit_logs_operation_status",
                "operation_status IN ('Succeeded', 'Rejected', 'Failed')");
        });

        builder.HasKey(log => log.Id);

        builder.Property(log => log.Id)
            .IsRequired()
            .HasColumnName("id");

        builder.Property(log => log.OccurredAt)
            .IsRequired()
            .HasColumnName("occurred_at");

        builder.Property(log => log.OperationType)
            .IsRequired()
            .HasMaxLength(64)
            .HasColumnName("operation_type");

        builder.Property(log => log.OperationStatus)
            .IsRequired()
            .HasMaxLength(16)
            .HasColumnName("operation_status");

        builder.Property(log => log.Channel)
            .IsRequired()
            .HasMaxLength(32)
            .HasColumnName("channel");

        builder.Property(log => log.BrandId)
            .HasColumnName("brand_id");

        builder.Property(log => log.ActorUserId)
            .HasColumnName("actor_user_id");

        builder.Property(log => log.CustomerUserId)
            .HasColumnName("customer_user_id");

        builder.Property(log => log.TargetEntityType)
            .HasMaxLength(64)
            .HasColumnName("target_entity_type");

        builder.Property(log => log.TargetEntityId)
            .HasColumnName("target_entity_id");

        builder.Property(log => log.Amount)
            .HasColumnName("amount");

        builder.Property(log => log.BalanceBefore)
            .HasColumnName("balance_before");

        builder.Property(log => log.BalanceAfter)
            .HasColumnName("balance_after");

        builder.Property(log => log.ReasonCode)
            .HasMaxLength(128)
            .HasColumnName("reason_code");

        builder.Property(log => log.Comment)
            .HasMaxLength(512)
            .HasColumnName("comment");

        builder.Property(log => log.TraceId)
            .HasMaxLength(128)
            .HasColumnName("trace_id");

        builder.Property(log => log.MetadataJson)
            .HasColumnType("jsonb")
            .HasColumnName("metadata_json");

        builder.Property(log => log.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(log => log.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(log => log.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasIndex(log => log.OccurredAt);
        builder.HasIndex(log => new { log.BrandId, log.OccurredAt });
        builder.HasIndex(log => new { log.ActorUserId, log.OccurredAt });
        builder.HasIndex(log => new { log.CustomerUserId, log.OccurredAt });
        builder.HasIndex(log => new { log.OperationType, log.OperationStatus, log.OccurredAt });
    }
}

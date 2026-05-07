using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.User;

namespace StampService.Infrastructure.Configurations;

public class RedemptionCodeConfiguration : IEntityTypeConfiguration<RedemptionCode>
{
    public void Configure(EntityTypeBuilder<RedemptionCode> builder)
    {
        builder.ToTable("redemption_codes");

        builder.HasKey(code => code.Id);

        builder.Property(code => code.Id)
            .IsRequired()
            .HasColumnName("id");

        builder.Property(code => code.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(code => code.Code)
            .IsRequired()
            .HasMaxLength(RedemptionCode.CodeLength)
            .HasColumnName("code");

        builder.Property(code => code.ExpiresAtUtc)
            .IsRequired()
            .HasColumnName("expires_at_utc");

        builder.Property(code => code.UsedAtUtc)
            .HasColumnName("used_at_utc")
            .IsConcurrencyToken();

        builder.HasOne(code => code.User)
            .WithMany()
            .HasForeignKey(code => code.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(code => code.Code);

        builder.HasIndex(code => new
        {
            code.UserId,
            code.UsedAtUtc,
            code.ExpiresAtUtc
        });

        builder.Property(code => code.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(code => code.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(code => code.DeletedAt)
            .HasColumnName("deleted_at");
    }
}

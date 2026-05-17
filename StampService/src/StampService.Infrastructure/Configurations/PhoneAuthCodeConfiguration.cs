using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.User;

namespace StampService.Infrastructure.Configurations;

public class PhoneAuthCodeConfiguration : IEntityTypeConfiguration<PhoneAuthCode>
{
    public void Configure(EntityTypeBuilder<PhoneAuthCode> builder)
    {
        builder.ToTable("phone_auth_codes");

        builder.HasKey(code => code.Id);

        builder.Property(code => code.Id)
            .IsRequired()
            .HasColumnName("id");

        builder.Property(code => code.PhoneNumber)
            .IsRequired()
            .HasMaxLength(PhoneAuthCode.MaxPhoneLength)
            .HasColumnName("phone_number");

        builder.Property(code => code.Code)
            .IsRequired()
            .HasMaxLength(PhoneAuthCode.CodeLength)
            .HasColumnName("code");

        builder.Property(code => code.ExpiresAtUtc)
            .IsRequired()
            .HasColumnName("expires_at_utc");

        builder.Property(code => code.UsedAtUtc)
            .HasColumnName("used_at_utc")
            .IsConcurrencyToken();

        builder.Property(code => code.FailedAttempts)
            .IsRequired()
            .HasColumnName("failed_attempts");

        builder.HasIndex(code => new
        {
            code.PhoneNumber,
            code.UsedAtUtc,
            code.ExpiresAtUtc
        }).HasDatabaseName("ix_phone_auth_codes_phone_used_expires");

        builder.Property(code => code.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(code => code.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(code => code.DeletedAt)
            .HasColumnName("deleted_at");
    }
}

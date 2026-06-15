using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.User;

namespace StampService.Infrastructure.Configurations;

public class PhoneAuthSmsSettingsConfiguration : IEntityTypeConfiguration<PhoneAuthSmsSettings>
{
    public void Configure(EntityTypeBuilder<PhoneAuthSmsSettings> builder)
    {
        builder.ToTable("phone_auth_sms_settings", table =>
        {
            table.HasCheckConstraint("ck_phone_auth_sms_settings_singleton", "id = 1");
        });

        builder.HasKey(settings => settings.Id);

        builder.Property(settings => settings.Id)
            .IsRequired()
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(settings => settings.IsEnabled)
            .IsRequired()
            .HasColumnName("is_enabled");
    }
}

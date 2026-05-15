using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.CustomerNotifications;

namespace StampService.Infrastructure.Configurations;

public class RewardDigestSettingsConfiguration : IEntityTypeConfiguration<RewardDigestSettings>
{
    public void Configure(EntityTypeBuilder<RewardDigestSettings> builder)
    {
        builder.ToTable("reward_digest_settings", table =>
        {
            table.HasCheckConstraint("ck_reward_digest_settings_singleton", "id = 1");
            table.HasCheckConstraint("ck_reward_digest_settings_message_interval_positive", "message_to_user_interval_minutes > 0");
            table.HasCheckConstraint("ck_reward_digest_settings_scan_interval_positive", "scan_interval_minutes > 0");
            table.HasCheckConstraint("ck_reward_digest_settings_batch_size_positive", "batch_size > 0");
            table.HasCheckConstraint("ck_reward_digest_settings_max_brands_positive", "max_brands_per_message > 0");
            table.HasCheckConstraint("ck_reward_digest_settings_max_rewards_positive", "max_rewards_per_brand > 0");
        });

        builder.HasKey(settings => settings.Id);

        builder.Property(settings => settings.Id)
            .IsRequired()
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(settings => settings.Enabled)
            .IsRequired()
            .HasColumnName("enabled");

        builder.Property(settings => settings.MessageToUserIntervalMinutes)
            .IsRequired()
            .HasColumnName("message_to_user_interval_minutes");

        builder.Property(settings => settings.ScanIntervalMinutes)
            .IsRequired()
            .HasColumnName("scan_interval_minutes");

        builder.Property(settings => settings.BatchSize)
            .IsRequired()
            .HasColumnName("batch_size");

        builder.Property(settings => settings.MaxBrandsPerMessage)
            .IsRequired()
            .HasColumnName("max_brands_per_message");

        builder.Property(settings => settings.MaxRewardsPerBrand)
            .IsRequired()
            .HasColumnName("max_rewards_per_brand");
    }
}

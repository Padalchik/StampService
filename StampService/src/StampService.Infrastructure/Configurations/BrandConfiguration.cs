using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.Brand;

namespace StampService.Infrastructure.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .IsRequired()
            .HasColumnName("id");

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(Constants.MAX_BRAND_NAME_LENGTH)
            .HasColumnName("name");

        builder.Property(b => b.IsMetricsEnabled)
            .IsRequired()
            .HasDefaultValue(true)
            .HasColumnName("is_metrics_enabled");

        builder.Property(b => b.IsCoinsEnabled)
            .IsRequired()
            .HasDefaultValue(true)
            .HasColumnName("is_coins_enabled");

        builder.Property(b => b.IsCoinProductRedemptionEnabled)
            .IsRequired()
            .HasDefaultValue(true)
            .HasColumnName("is_coin_product_redemption_enabled");

        builder.Property(b => b.IsManualCoinRedemptionEnabled)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("is_manual_coin_redemption_enabled");

        builder.Property(b => b.IsWelcomeRewardsEnabled)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("is_welcome_rewards_enabled");

        builder.Property(b => b.WelcomeCoinsAmount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasColumnName("welcome_coins_amount");

        builder.Property(b => b.WelcomeRewardComment)
            .IsRequired()
            .HasMaxLength(200)
            .HasDefaultValue("Приветственная награда")
            .HasColumnName("welcome_reward_comment");

        builder.OwnsMany(b => b.WelcomeMetricRewards, rewardBuilder =>
        {
            rewardBuilder.ToTable("brand_welcome_metric_rewards");

            rewardBuilder.WithOwner()
                .HasForeignKey("brand_id");

            rewardBuilder.HasKey("brand_id", nameof(BrandWelcomeMetricReward.MetricDefinitionId));

            rewardBuilder.Property(reward => reward.MetricDefinitionId)
                .IsRequired()
                .HasColumnName("metric_definition_id");

            rewardBuilder.Property(reward => reward.Amount)
                .IsRequired()
                .HasColumnName("amount");
        });

        builder.Navigation(b => b.WelcomeMetricRewards)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(b => b.Locations)
            .WithOne(l => l.Brand)
            .HasForeignKey(l => l.BrandId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(b => b.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(b => b.DeletedAt)
            .HasColumnName("deleted_at");
    }
}

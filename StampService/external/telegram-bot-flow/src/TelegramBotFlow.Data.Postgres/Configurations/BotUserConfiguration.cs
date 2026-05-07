using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TelegramBotFlow.Core.Data.Configurations;

public sealed class BotUserConfiguration : IEntityTypeConfiguration<BotUser>
{
    public void Configure(EntityTypeBuilder<BotUser> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.TelegramId);

        builder.Property(x => x.TelegramId)
           .HasColumnName("telegram_id")
           .ValueGeneratedNever();

        builder.Property(x => x.JoinedAt)
           .HasColumnName("joined_at")
           .IsRequired();

        builder.Property(x => x.IsBlocked)
           .HasColumnName("is_blocked")
           .HasDefaultValue(false);
    }
}
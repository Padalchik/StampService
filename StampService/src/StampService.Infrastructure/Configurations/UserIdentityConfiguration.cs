using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.User;

namespace StampService.Infrastructure.Configurations;

public class UserIdentityConfiguration : IEntityTypeConfiguration<UserIdentity>
{
    public void Configure(EntityTypeBuilder<UserIdentity> builder)
    {
        builder.ToTable("user_identities");

        builder.HasKey(ui => ui.Id);

        builder.Property(ui => ui.Id)
            .IsRequired()
            .HasColumnName("id");

        builder.Property(ui => ui.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(ui => ui.Type)
            .IsRequired()
            .HasColumnName("identity_type");

        builder.Property(ui => ui.Key)
            .IsRequired()
            .HasMaxLength(Constants.MAX_IDENTITY_KEY_LENGTH)
            .HasColumnName("key");

        builder.Property(ui => ui.Metadata)
            .IsRequired()
            .HasMaxLength(Constants.MAX_IDENTITY_METADATA_LENGTH)
            .HasColumnType("jsonb")
            .HasColumnName("metadata");

        builder.HasIndex(ui => new { ui.Type, ui.Key })
            .IsUnique()
            .HasFilter("\"deleted_at\" IS NULL")
            .HasDatabaseName("ix_user_identities_type_key");

        builder.HasOne(ui => ui.User)
            .WithMany(u => u.Identities)
            .HasForeignKey(ui => ui.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(ui => ui.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(ui => ui.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(ui => ui.DeletedAt)
            .HasColumnName("deleted_at");
    }
}

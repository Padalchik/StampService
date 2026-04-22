using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.Access;

namespace StampService.Infrastructure.Configurations;

public class BrandMembershipConfiguration : IEntityTypeConfiguration<BrandMembership>
{
    public void Configure(EntityTypeBuilder<BrandMembership> builder)
    {
        builder.ToTable("brand_memberships");

        builder.HasKey(bm => bm.Id);

        builder.Property(bm => bm.Id)
            .IsRequired()
            .HasColumnName("id");

        builder.Property(bm => bm.UserId)
            .IsRequired()
            .HasColumnName("user_id");

        builder.Property(bm => bm.BrandId)
            .IsRequired()
            .HasColumnName("brand_id");

        builder.Property(bm => bm.RoleId)
            .IsRequired()
            .HasColumnName("role_id");

        builder.HasIndex(bm => new { bm.UserId, bm.BrandId })
            .IsUnique()
            .HasDatabaseName("ix_brand_memberships_user_id_brand_id");

        builder.HasOne(bm => bm.User)
            .WithMany()
            .HasForeignKey(bm => bm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bm => bm.Brand)
            .WithMany()
            .HasForeignKey(bm => bm.BrandId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bm => bm.Role)
            .WithMany()
            .HasForeignKey(bm => bm.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(bm => bm.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(bm => bm.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(bm => bm.DeletedAt)
            .HasColumnName("deleted_at");
    }
}

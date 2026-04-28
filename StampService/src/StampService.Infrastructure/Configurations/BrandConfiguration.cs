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

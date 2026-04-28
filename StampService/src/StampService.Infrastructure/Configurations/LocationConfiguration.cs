using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StampService.Domain.Brand;

namespace StampService.Infrastructure.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .IsRequired()
            .HasColumnName("id");

        builder.Property(l => l.BrandId)
            .IsRequired()
            .HasColumnName("brand_id");

        builder.ComplexProperty(l => l.Name, nb =>
        {
            nb.Property(n => n.Name)
                .IsRequired()
                .HasMaxLength(Constants.MAX_LOCATION_NAME_LENGTH)
                .HasColumnName("name");
        });

        builder.ComplexProperty(l => l.Address, ab =>
        {
            ab.Property(a => a.City)
                .IsRequired()
                .HasMaxLength(Constants.MAX_ADDRESS_CITY_LENGTH)
                .HasColumnName("city");

            ab.Property(a => a.Street)
                .IsRequired()
                .HasMaxLength(Constants.MAX_ADDRESS_STREET_LENGTH)
                .HasColumnName("street");

            ab.Property(a => a.HouseNumber)
                .IsRequired()
                .HasMaxLength(Constants.MAX_ADDRESS_HOUSE_NUMBER_LENGTH)
                .HasColumnName("house_number");
        });

        builder.Property(l => l.IsActive)
            .IsRequired()
            .HasColumnName("is_active");

        builder.HasOne(l => l.Brand)
            .WithMany(b => b.Locations)
            .HasForeignKey(l => l.BrandId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(l => l.DeletedAt)
            .HasColumnName("deleted_at");
    }
}

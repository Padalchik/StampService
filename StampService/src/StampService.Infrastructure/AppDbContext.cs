using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StampService.Domain.Access;
using StampService.Domain.Brand;
using StampService.Domain.Coins;
using StampService.Domain.Loyalty;
using StampService.Domain.Shared;

namespace StampService.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global Query Filter для Soft Delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, "DeletedAt");
                var nullValue = Expression.Constant(null, typeof(DateTime?));
                var equal = Expression.Equal(property, nullValue);
                var lambda = Expression.Lambda(equal, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    // User & Identity
    public DbSet<StampService.Domain.User.User> Users => Set<StampService.Domain.User.User>();
    public DbSet<StampService.Domain.User.UserIdentity> UserIdentities => Set<StampService.Domain.User.UserIdentity>();
    public DbSet<StampService.Domain.User.RedemptionCode> RedemptionCodes => Set<StampService.Domain.User.RedemptionCode>();

    // Brand
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Location> Locations => Set<Location>();

    // Access
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<BrandMembership> BrandMemberships => Set<BrandMembership>();

    // Loyalty
    public DbSet<LoyaltyMetricDefinition> LoyaltyMetricDefinitions => Set<LoyaltyMetricDefinition>();
    public DbSet<MetricBalance> MetricBalances => Set<MetricBalance>();
    public DbSet<StampTransaction> StampTransactions => Set<StampTransaction>();

    // Coins
    public DbSet<CoinWallet> CoinWallets => Set<CoinWallet>();
    public DbSet<CoinTransaction> CoinTransactions => Set<CoinTransaction>();
}

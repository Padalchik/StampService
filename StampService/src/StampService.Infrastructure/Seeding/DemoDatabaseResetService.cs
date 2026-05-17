using Microsoft.EntityFrameworkCore;
using StampService.Application.Demo;

namespace StampService.Infrastructure.Seeding;

public class DemoDatabaseResetService : IDemoDatabaseResetService
{
    private readonly AppDbContext _dbContext;

    public DemoDatabaseResetService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                brand_memberships,
                coin_products,
                coin_transactions,
                coin_wallets,
                customer_digest_states,
                loyalty_metric_definitions,
                metric_balances,
                redemption_codes,
                reward_digest_settings,
                roles,
                stamp_transactions,
                user_identities,
                users,
                locations,
                brands
            RESTART IDENTITY CASCADE;
            """,
            cancellationToken);

        await RoleSeeder.SeedSystemRolesAsync(_dbContext, cancellationToken);
    }
}

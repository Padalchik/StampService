using Microsoft.EntityFrameworkCore;
using StampService.Application.Audit;

namespace StampService.Infrastructure.Repositories;

public sealed class BusinessAuditLogRepository : IBusinessAuditLogRepository
{
    private readonly AppDbContext _dbContext;

    public BusinessAuditLogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BusinessAuditLogPage> GetAsync(
        BusinessAuditLogFilter filter,
        CancellationToken cancellationToken)
    {
        var query =
            from log in _dbContext.BusinessAuditLogs.AsNoTracking()
            join brand in _dbContext.Brands.AsNoTracking()
                on log.BrandId equals brand.Id into brands
            from brand in brands.DefaultIfEmpty()
            join actor in _dbContext.Users.AsNoTracking()
                on log.ActorUserId equals actor.Id into actors
            from actor in actors.DefaultIfEmpty()
            join customer in _dbContext.Users.AsNoTracking()
                on log.CustomerUserId equals customer.Id into customers
            from customer in customers.DefaultIfEmpty()
            select new
            {
                Log = log,
                BrandName = brand == null ? null : brand.Name,
                ActorName = actor == null ? null : actor.Name,
                CustomerName = customer == null ? null : customer.Name
            };

        if (filter.OccurredFromUtc is { } from)
            query = query.Where(item => item.Log.OccurredAt >= from);

        if (filter.OccurredToUtc is { } to)
            query = query.Where(item => item.Log.OccurredAt <= to);

        if (filter.BrandId is { } brandId)
            query = query.Where(item => item.Log.BrandId == brandId);

        if (filter.CustomerUserId is { } customerUserId)
            query = query.Where(item => item.Log.CustomerUserId == customerUserId);

        if (!string.IsNullOrWhiteSpace(filter.ActorName))
        {
            var actorName = filter.ActorName.Trim().ToLower();
            query = query.Where(item => item.ActorName != null && item.ActorName.ToLower().Contains(actorName));
        }

        if (!string.IsNullOrWhiteSpace(filter.OperationType))
            query = query.Where(item => item.Log.OperationType == filter.OperationType);

        if (!string.IsNullOrWhiteSpace(filter.OperationStatus))
            query = query.Where(item => item.Log.OperationStatus == filter.OperationStatus);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.Log.OccurredAt)
            .Take(filter.Take)
            .Select(item => new BusinessAuditLogReadModel(
                item.Log.OccurredAt,
                item.Log.OperationType,
                item.Log.OperationStatus,
                item.Log.Channel,
                item.Log.BrandId,
                item.BrandName,
                item.Log.ActorUserId,
                item.ActorName,
                item.Log.CustomerUserId,
                item.CustomerName,
                item.Log.TargetEntityType,
                item.Log.TargetEntityId,
                item.Log.Amount,
                item.Log.BalanceBefore,
                item.Log.BalanceAfter,
                item.Log.ReasonCode,
                item.Log.Comment))
            .ToArrayAsync(cancellationToken);

        return new BusinessAuditLogPage(items, totalCount);
    }
}

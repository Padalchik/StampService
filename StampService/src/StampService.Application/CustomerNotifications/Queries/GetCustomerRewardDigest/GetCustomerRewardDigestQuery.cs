using StampService.Application.Abstractions;

namespace StampService.Application.CustomerNotifications.Queries.GetCustomerRewardDigest;

public record GetCustomerRewardDigestQuery(
    Guid UserId,
    int MaxBrands,
    int MaxRewardsPerBrand) : IQuery;

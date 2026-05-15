using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.CustomerNotifications;
using StampService.Application.Errors;

namespace StampService.Application.CustomerNotifications.Queries.GetCustomerRewardDigest;

public class GetCustomerRewardDigestHandler
    : IQueryHandler<CustomerRewardDigest, GetCustomerRewardDigestQuery>
{
    private readonly ICustomerRewardDigestRepository _digestRepository;

    public GetCustomerRewardDigestHandler(ICustomerRewardDigestRepository digestRepository)
    {
        _digestRepository = digestRepository;
    }

    public async Task<Result<CustomerRewardDigest>> Handle(
        GetCustomerRewardDigestQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var digest = await _digestRepository.GetAvailableRewardsAsync(
            query.UserId,
            query.MaxBrands,
            query.MaxRewardsPerBrand,
            cancellationToken);

        return Result.Ok(digest);
    }
}

using StampService.Application.Abstractions;

namespace StampService.Application.Metrics.Queries.GetRedeemMetricOptions;

public record GetRedeemMetricOptionsQuery(
    Guid RedeemerUserId,
    Guid BrandId,
    string RedemptionCode) : IQuery;

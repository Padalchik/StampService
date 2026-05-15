using StampService.Application.Abstractions;

namespace StampService.Application.CustomerNotifications.Queries.GetRewardDigestSettings;

public record GetRewardDigestSettingsQuery(long AdminTelegramUserId) : IQuery;

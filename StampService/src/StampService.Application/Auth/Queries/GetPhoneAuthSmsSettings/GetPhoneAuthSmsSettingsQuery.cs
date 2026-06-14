using StampService.Application.Abstractions;
using StampService.Application.Administration;

namespace StampService.Application.Auth.Queries.GetPhoneAuthSmsSettings;

public record GetPhoneAuthSmsSettingsQuery(AdminActor AdminActor) : IQuery;

using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Brands.Queries.GetBrandStaff;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Brands;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Features.Staff.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Staff.Screens;

public sealed class BrandStaffListScreen : IScreen
{
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;
    private readonly IQueryHandler<IReadOnlyCollection<BrandStaffResponse>, GetBrandStaffQuery> _staffHandler;

    public BrandStaffListScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<IReadOnlyCollection<BrandStaffResponse>, GetBrandStaffQuery> staffHandler)
    {
        _ensureUserHandler = ensureUserHandler;
        _staffHandler = staffHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandId = StaffBrandContext.GetBrandId(ctx);
        if (brandId == Guid.Empty)
            return new ScreenView("Бренд не выбран.").BackButton();

        var actorUserId = await GetActorUserIdAsync(ctx);
        if (actorUserId is null)
            return new ScreenView("Не удалось определить пользователя.").BackButton();

        var result = await _staffHandler.Handle(
            new GetBrandStaffQuery(actorUserId.Value, brandId),
            ctx.CancellationToken);

        if (result.IsFailed)
            return new ScreenView($"Не удалось загрузить сотрудников: {BotErrorFormatter.Format(result.Errors)}").BackButton();

        var brandName = StaffBrandContext.GetBrandName(ctx);
        var view = new ScreenView(
            $"<b>Сотрудники</b>\n{Html(brandName)}\n\n" +
            (result.Value.Count == 0
                ? "Сотрудников пока нет."
                : "Выберите сотрудника:"));

        foreach (var staff in result.Value)
        {
            var phoneNumber = staff.PhoneNumber ?? "-";
            view.Row().Button<OpenStaffDetailsAction, OpenStaffDetailsPayload>(
                $"{staff.UserName} · {phoneNumber}",
                new OpenStaffDetailsPayload(
                    staff.UserId,
                    staff.UserName,
                    staff.PhoneNumber));
        }

        return view.Row()
            .Button<StartAddStaffAction>("Добавить сотрудника")
            .Row()
            .BackButton();
    }

    private async Task<Guid?> GetActorUserIdAsync(UpdateContext ctx)
    {
        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var result = await _ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        return result.IsSuccess ? result.Value.UserId : null;
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}

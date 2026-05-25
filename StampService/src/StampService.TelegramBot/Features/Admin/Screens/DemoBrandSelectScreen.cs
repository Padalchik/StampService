using StampService.Application.Abstractions;
using StampService.Application.Brands.Queries.GetAdminBrands;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Brands;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Features.Admin.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Admin.Screens;

public sealed class DemoBrandSelectScreen : IScreen
{
    private readonly IQueryHandler<IReadOnlyCollection<AdminBrandResponse>, GetAdminBrandsQuery> _brandsHandler;
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;

    public DemoBrandSelectScreen(
        IQueryHandler<IReadOnlyCollection<AdminBrandResponse>, GetAdminBrandsQuery> brandsHandler,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler)
    {
        _brandsHandler = brandsHandler;
        _ensureUserHandler = ensureUserHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var userResult = await _ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);
        if (userResult.IsFailed)
            return new ScreenView($"Не удалось определить пользователя: {BotErrorFormatter.Format(userResult.Errors)}").BackButton();

        var brandsResult = await _brandsHandler.Handle(
            new GetAdminBrandsQuery(ctx.UserId),
            ctx.CancellationToken);
        if (brandsResult.IsFailed)
            return new ScreenView($"Нет доступа к брендам: {BotErrorFormatter.Format(brandsResult.Errors)}").BackButton();

        var ownedBrands = brandsResult.Value
            .Where(brand => brand.OwnerUserId == userResult.Value.UserId)
            .OrderBy(brand => brand.BrandName)
            .ToArray();

        if (ownedBrands.Length == 0)
        {
            return new ScreenView(
                "<b>Брендов для демо нет</b>\n\n" +
                "Демо-данные можно создать только в бренде, где вы являетесь владельцем.")
                .MenuButton("В главное меню");
        }

        var phoneNumber = ctx.Session?.Data.GetString(AdminSessionKeys.DemoPhoneNumber) ?? "-";
        var view = new ScreenView(
            "<b>Выберите бренд</b>\n\n" +
            $"Пользователь: <code>{phoneNumber}</code>");

        foreach (var brand in ownedBrands)
        {
            view.Row().Button<SelectDemoBrandAction, SelectDemoBrandPayload>(
                brand.BrandName,
                new SelectDemoBrandPayload(brand.BrandId, brand.BrandName));
        }

        return view.Row().MenuButton("В главное меню");
    }
}

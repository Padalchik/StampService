using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Administration;
using StampService.Application.Auth;
using StampService.Application.Demo.Commands.CreateDemoBrands;
using StampService.Application.Demo.Commands.CreateUserDemoData;
using StampService.Application.Demo.Commands.ResetDemoDatabase;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Common.Routing;
using StampService.TelegramBot.Features.Admin.Actions;
using StampService.TelegramBot.Features.Admin.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Admin.Endpoints;

public sealed class AdminDemoEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<StartDemoResetAction>(StartResetAsync);
        app.MapAction<ConfirmDemoResetAction>(ConfirmResetAsync);
        app.MapAction<CancelDemoResetAction>(CancelResetAsync);
        app.MapAction<CreateDemoBrandsAction>(CreateDemoBrandsAsync);
        app.MapAction<StartCreateUserDemoDataAction>(StartCreateUserDemoDataAsync);
        app.MapInput<EnterDemoPhoneAction>(EnterPhoneAsync);
        app.MapAction<SelectDemoBrandAction, SelectDemoBrandPayload>(SelectBrandAsync);
    }

    private static Task<IEndpointResult> StartResetAsync(UpdateContext ctx)
    {
        return Task.FromResult(BotResults.NavigateTo<DemoResetConfirmScreen>());
    }

    private static async Task<IEndpointResult> ConfirmResetAsync(
        UpdateContext ctx,
        ICommandHandler<bool, ResetDemoDatabaseCommand> handler)
    {
        var result = await handler.Handle(
            new ResetDemoDatabaseCommand(AdminActor.FromTelegram(ctx.UserId)),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось очистить БД: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        ctx.Session?.Clear();

        return BotResults.ShowView(new ScreenView(
            "<b>БД очищена</b>\n\n" +
            "Системные роли восстановлены. Откройте главное меню заново.")
            .MenuButton("В главное меню"));
    }

    private static Task<IEndpointResult> CancelResetAsync(UpdateContext ctx)
    {
        return Task.FromResult(BotResults.NavigateTo<AdminDemoScreen>());
    }

    private static async Task<IEndpointResult> CreateDemoBrandsAsync(
        UpdateContext ctx,
        ICommandHandler<bool, CreateDemoBrandsCommand> handler)
    {
        var result = await handler.Handle(
            new CreateDemoBrandsCommand(AdminActor.FromTelegram(ctx.UserId)),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось создать демо-бренды: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        return BotResults.ShowView(new ScreenView(
            "<b>Демо-бренды созданы</b>\n\n" +
            "Добавлены бренды с разными настройками, метрики и товары.")
            .MenuButton("В главное меню"));
    }

    private static Task<IEndpointResult> StartCreateUserDemoDataAsync(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(AdminSessionKeys.DemoPhoneNumber);
        return Task.FromResult(BotResults.NavigateTo<DemoPhoneScreen>());
    }

    private static Task<IEndpointResult> EnterPhoneAsync(UpdateContext ctx)
    {
        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(ctx.MessageText, "PhoneNumber");
        if (phoneNumberResult.IsFailed)
        {
            return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                    "Введите корректный номер телефона.")
                .AwaitInput<EnterDemoPhoneAction>()
                .BackButton())));
        }

        ctx.Session?.Data.Set(AdminSessionKeys.DemoPhoneNumber, phoneNumberResult.Value);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<DemoBrandSelectScreen>()));
    }

    private static async Task<IEndpointResult> SelectBrandAsync(
        UpdateContext ctx,
        SelectDemoBrandPayload payload,
        ICommandHandler<bool, CreateUserDemoDataCommand> handler)
    {
        var phoneNumber = ctx.Session?.Data.GetString(AdminSessionKeys.DemoPhoneNumber) ?? string.Empty;
        if (PhoneNumberNormalizer.NormalizeForAuth(phoneNumber).IsFailed)
            return BotResults.ShowView(new ScreenView("Сценарий создания демо-данных устарел. Начните заново.").BackButton());

        var result = await handler.Handle(
            new CreateUserDemoDataCommand(AdminActor.FromTelegram(ctx.UserId), phoneNumber, payload.BrandId),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось создать демо-данные: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        ctx.Session?.Data.Remove(AdminSessionKeys.DemoPhoneNumber);

        return BotResults.ShowView(new ScreenView(
            "<b>Демо-данные созданы</b>\n\n" +
            $"Пользователь: <code>{Html(phoneNumber)}</code>\n" +
            $"Бренд: {Html(payload.BrandName)}\n\n" +
            "Добавлены товары, метрики, балансы и история начислений/списаний.")
            .MenuButton("В главное меню"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}

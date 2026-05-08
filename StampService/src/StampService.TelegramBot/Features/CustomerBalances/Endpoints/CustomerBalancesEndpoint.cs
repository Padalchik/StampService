using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Metrics.Queries.GetBrandCustomerMetricBalances;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Common.Routing;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.CustomerBalances.Actions;
using StampService.TelegramBot.Features.CustomerBalances.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;
using UserEntity = StampService.Domain.User.User;

namespace StampService.TelegramBot.Features.CustomerBalances.Endpoints;

public sealed class CustomerBalancesEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<StartCustomerBalancesAction>(StartAsync);
        app.MapInput<EnterCustomerBalancesCodeAction>(EnterCustomerCodeAsync);
    }

    private static Task<IEndpointResult> StartAsync(UpdateContext ctx)
    {
        return Task.FromResult(BotResults.NavigateTo<CustomerBalancesCodeScreen>());
    }

    private static async Task<IEndpointResult> EnterCustomerCodeAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<BrandCustomerMetricBalancesResponse, GetBrandCustomerMetricBalancesQuery> balancesHandler)
    {
        var brandId = ctx.Session?.Data.Get<Guid>(BrandWorkspaceScreen.BrandIdSessionKey) ?? Guid.Empty;
        if (brandId == Guid.Empty)
            return BotResults.ShowView(new ScreenView("Бренд не выбран.").BackButton());

        var customerCode = ctx.MessageText?.Trim() ?? string.Empty;
        if (!UserEntity.IsValidCustomerCode(customerCode))
        {
            return BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                "CustomerCode должен состоять из 4 цифр.")
                .AwaitInput<EnterCustomerBalancesCodeAction>()
                .BackButton()));
        }

        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var userResult = await ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        if (userResult.IsFailed)
        {
            return BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                $"Не удалось определить пользователя: {BotErrorFormatter.Format(userResult.Errors)}")
                .AwaitInput<EnterCustomerBalancesCodeAction>()
                .BackButton()));
        }

        var balancesResult = await balancesHandler.Handle(
            new GetBrandCustomerMetricBalancesQuery(
                userResult.Value.UserId,
                brandId,
                customerCode),
            ctx.CancellationToken);

        if (balancesResult.IsFailed)
        {
            return BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                $"Не удалось загрузить балансы: {BotErrorFormatter.Format(balancesResult.Errors)}")
                .AwaitInput<EnterCustomerBalancesCodeAction>()
                .BackButton()));
        }

        return BotInputResults.DeleteInputThen(BotResults.ShowView(BuildBalancesView(ctx, balancesResult.Value)));
    }

    private static ScreenView BuildBalancesView(
        UpdateContext ctx,
        BrandCustomerMetricBalancesResponse response)
    {
        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        var activeBalances = response.Balances
            .Where(balance => balance.IsActive)
            .ToArray();

        IEnumerable<string> lines = activeBalances.Length == 0
            ? ["В бренде пока нет метрик."]
            : activeBalances.Select(balance => $"{Html(balance.MetricName)}: {balance.Value}");

        return new ScreenView(
            $"<b>{Html(brandName)}</b>\n\n" +
            $"Клиент: {Html(response.CustomerName)} · <code>{Html(response.CustomerCode)}</code>\n\n" +
            string.Join("\n", lines))
            .NavigateButton<CustomerBalancesCodeScreen>("Другой клиент")
            .Row()
            .NavigateButton<BrandWorkspaceScreen>("К бренду")
            .BackButton();
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}

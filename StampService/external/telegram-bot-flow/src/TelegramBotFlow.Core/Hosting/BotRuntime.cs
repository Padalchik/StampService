using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Hosting;

internal sealed class BotRuntime
{
    private readonly WebApplication _app;
    private readonly IServiceProvider _services;

    public BotRuntime(WebApplication app)
    {
        _app = app;
        _services = app.Services;
    }

    public async Task RunAsync(UpdatePipeline pipeline, MenuBuilder? menuBuilder)
    {
        ReplaceUpdatePipeline(_services, pipeline);

        BotConfiguration config = _services.GetRequiredService<IOptions<BotConfiguration>>().Value;

        if (config.Mode == BotMode.WEBHOOK)
            await ConfigureWebhookAsync(config, pipeline);

        if (menuBuilder is not null)
            await ApplyMenuAsync(menuBuilder);

        _app.MapGet(config.HealthCheckPath, () => Results.Ok(new { status = "healthy" }));

        await _app.RunAsync();
    }

    private async Task ConfigureWebhookAsync(BotConfiguration config, UpdatePipeline pipeline)
    {
        _app.MapPost(config.WebhookPath, async (
           Update update,
           HttpContext httpContext,
           IServiceProvider sp,
           CancellationToken ct) =>
       {
           if (!string.IsNullOrEmpty(config.WebhookSecretToken))
           {
               string? header = httpContext.Request.Headers["X-Telegram-Bot-Api-Secret-Token"];
               if (header != config.WebhookSecretToken)
                   return Results.StatusCode(403);
           }

           await WebhookEndpoints.HandleWebhookUpdate(update, pipeline, sp, ct);
           return Results.Ok();
       });

        ITelegramBotClient bot = _services.GetRequiredService<ITelegramBotClient>();
        await bot.SetWebhook(config.WebhookUrl + config.WebhookPath,
            allowedUpdates: config.AllowedUpdates,
            secretToken: config.WebhookSecretToken);
    }

    private async Task ApplyMenuAsync(MenuBuilder menuBuilder)
    {
        ITelegramBotClient bot = _services.GetRequiredService<ITelegramBotClient>();
        BotConfiguration config = _services.GetRequiredService<IOptions<BotConfiguration>>().Value;
        await menuBuilder.ApplyAsync(bot, config.AdminUserIds);
    }

    private static void ReplaceUpdatePipeline(IServiceProvider services, UpdatePipeline pipeline)
    {
        PipelineHolder holder = services.GetRequiredService<PipelineHolder>();
        holder.Pipeline = pipeline;
    }
}
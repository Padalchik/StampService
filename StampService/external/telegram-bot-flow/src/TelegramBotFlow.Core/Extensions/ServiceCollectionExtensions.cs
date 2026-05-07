using System.Net;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Http;
using TelegramBotFlow.Core.Messaging;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramBotFlow(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BotConfiguration>(configuration.GetSection(BotConfiguration.SECTION_NAME));
        services.Configure<BotMessages>(configuration.GetSection("Bot:Messages"));

        BotConfiguration botConfig = configuration.GetSection(BotConfiguration.SECTION_NAME).Get<BotConfiguration>()
                                     ?? throw new InvalidOperationException(
                                         $"Bot configuration section '{BotConfiguration.SECTION_NAME}' is missing or invalid.");

        var rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = botConfig.TelegramRateLimitPerSecond,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokensPerPeriod = botConfig.TelegramRateLimitPerSecond,
            QueueLimit = botConfig.TelegramRateLimitPerSecond * 10
        });

        services.AddHttpClient("telegram")
            .AddHttpMessageHandler(() => new TelegramRateLimitHandler(rateLimiter))
            .AddResilienceHandler("telegram-retry", builder =>
            {
                // Per-attempt timeout — caps how long a single Telegram HTTP call can block
                // before Polly fails the attempt. Without it, slow TG responses (50s+
                // observed in prod) blocked Wolverine handlers past their own timeout,
                // causing handler retries with the same payload → duplicate messages
                // delivered to users.
                if (botConfig.TelegramRequestTimeoutSeconds > 0)
                {
                    builder.AddTimeout(TimeSpan.FromSeconds(botConfig.TelegramRequestTimeoutSeconds));
                }

                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = botConfig.MaxRetryOnRateLimit,
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = args => ValueTask.FromResult(
                        args.Outcome.Result?.StatusCode is
                            HttpStatusCode.TooManyRequests or
                            HttpStatusCode.InternalServerError or
                            HttpStatusCode.ServiceUnavailable
                        // Timeouts из AddTimeout — тоже retry-able. Если TG отдаёт
                        // медленно из-за всплеска нагрузки, exponential backoff даст
                        // ему шанс восстановиться без эскалации к Wolverine retry.
                        || args.Outcome.Exception is Polly.Timeout.TimeoutRejectedException),
                    DelayGenerator = args =>
                    {
                        if (args.Outcome.Result?.Headers.RetryAfter?.Delta is { } delta)
                            return ValueTask.FromResult<TimeSpan?>(delta);
                        return ValueTask.FromResult<TimeSpan?>(null);
                    }
                });
            });

        services.AddSingleton<ITelegramBotClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("telegram");
            return new TelegramBotClient(botConfig.Token, httpClient);
        });

        services.AddSingleton<PipelineHolder>();
        services.AddSingleton(sp => sp.GetRequiredService<PipelineHolder>().Pipeline);

        services.AddSingleton<UpdateRouter>();
        services.TryAddSingleton<ScreenRegistry>();
        services.AddScoped<IUpdateResponder, UpdateResponder>();
        services.AddScoped<IUserAccessPolicy, BotConfigurationUserAccessPolicy>();
        services.AddScoped<IScreenMessageRenderer, ScreenMessageRenderer>();
        services.AddScoped<ScreenManager>();
        services.AddScoped<INavigationService, NavigationService>();

        services.AddSingleton<ISessionStore, InMemorySessionStore>();
        services.AddSingleton<ISessionLockProvider, InMemorySessionLockProvider>();

        services.AddSingleton<IBotNotifier, BotNotifier>();
        services.AddSingleton<IBotBroadcaster, BotBroadcaster>();
        services.AddSingleton<IChatAdministrationApi, ChatAdministrationApi>();

        services.AddSingleton<InputHandlerRegistry>();
        services.AddScoped<PendingInputMiddleware>();

        services.AddScoped<ErrorHandlingMiddleware>();
        services.AddScoped<LoggingMiddleware>();
        services.AddScoped<PrivateChatOnlyMiddleware>();
        services.AddScoped<SessionMiddleware>();
        services.AddScoped<AccessPolicyMiddleware>();

        Channel<Update> updateChannel = Channel.CreateBounded<Update>(new BoundedChannelOptions(botConfig.UpdateChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = false
        });
        services.AddSingleton(updateChannel.Writer);
        services.AddSingleton(updateChannel.Reader);

        if (botConfig.Mode == BotMode.POLLING)
            services.AddHostedService<PollingService>();

        services.AddHostedService<UpdateProcessingWorker>();

        services.Configure<HostOptions>(opts =>
            opts.ShutdownTimeout = TimeSpan.FromSeconds(botConfig.ShutdownTimeoutSeconds));

        return services;
    }

    public static IServiceCollection AddSessionStore<TStore>(this IServiceCollection services)
        where TStore : class, ISessionStore
    {
        services.RemoveAll<ISessionStore>();
        services.AddSingleton<ISessionStore, TStore>();

        return services;
    }

    public static IServiceCollection AddWizards(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddMemoryCache();
        services.AddSingleton<IWizardStore, InMemoryWizardStore>();
        services.AddScoped<IWizardLauncher, WizardLauncher>();
        services.AddScoped<WizardMiddleware>();

        WizardRegistry registry = new();

        foreach (Assembly assembly in assemblies)
        {
            foreach (Type wizardType in WizardRegistry.GetWizardTypes(assembly))
            {
                registry.Register(wizardType);
                services.TryAddScoped(wizardType);
            }
        }

        services.AddSingleton(registry);

        return services;
    }

    public static IServiceCollection AddScreens(this IServiceCollection services, Assembly assembly)
    {
        List<Type> screenTypes = [.. ScreenRegistry.GetScreenTypes(assembly)];

        foreach (Type screenType in screenTypes)
            services.TryAddScoped(screenType);

        services.RemoveAll<ScreenRegistry>();
        services.AddSingleton(_ =>
        {
            var registry = new ScreenRegistry();
            registry.RegisterFromAssembly(assembly);
            return registry;
        });

        return services;
    }
}
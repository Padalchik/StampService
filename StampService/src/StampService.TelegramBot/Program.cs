using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using StampService.Application;
using StampService.Application.Administration;
using StampService.Application.Audit;
using StampService.Application.CustomerNotifications;
using StampService.Infrastructure;
using StampService.TelegramBot.Common.Audit;
using StampService.Infrastructure.Seeding;
using StampService.TelegramBot.Common.Notifications;
using StampService.TelegramBot.Features.MainMenu.Screens;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Extensions;
using TelegramBotFlow.Core.Hosting;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = BotApplication.CreateBuilder(args);

    builder.Configuration.AddJsonFile(
        $"appsettings.{builder.WebAppBuilder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: true);

    builder.WebAppBuilder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
        ImportJwtConfigurationFromApiSettings(builder.Configuration, builder.WebAppBuilder.Environment);

    var telegramBotToken = builder.Configuration["Telegram:BotToken"];
    if (string.IsNullOrWhiteSpace(builder.Configuration["Bot:Token"])
        && !string.IsNullOrWhiteSpace(telegramBotToken))
    {
        builder.Configuration["Bot:Token"] = telegramBotToken;
    }

    if (string.IsNullOrWhiteSpace(builder.Configuration["Bot:Token"]))
        throw new InvalidOperationException(
            "Bot token is not configured. Set Telegram:BotToken in user-secrets or Bot:Token in configuration.");

    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is not configured. Run bot in Development or provide connection string.");

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddScoped<IBusinessAuditContext, TelegramBusinessAuditContext>();
    builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection("Admin"));
    builder.Services.Configure<BotStartupNotificationOptions>(
        builder.Configuration.GetSection(BotStartupNotificationOptions.SectionName));
    builder.Services.Configure<RewardDigestOptions>(builder.Configuration.GetSection(RewardDigestOptions.SectionName));
    builder.Services.AddScoped<
        StampService.TelegramBot.Common.Notifications.ICustomerNotificationService,
        CustomerNotificationService>();
    builder.Services.AddScoped<
        StampService.Application.CustomerNotifications.ICustomerNotificationService,
        CustomerNotificationApplicationAdapter>();
    builder.Services.AddScoped<CustomerRewardDigestSender>();
    builder.Services.AddHostedService<BotStartupNotificationHostedService>();
    builder.Services.AddHostedService<CustomerRewardDigestHostedService>();
    builder.Services.AddBotEndpoints(typeof(Program).Assembly);
    builder.Services.AddScreens(typeof(Program).Assembly);

    builder.UseErrorHandling();
    builder.UseLogging();
    builder.UsePrivateChatOnly();
    builder.UseSession();
    builder.UsePendingInput();

    var app = builder.Build();

    await using (var scope = app.Services.CreateAsyncScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (app.WebApp.Environment.IsDevelopment())
            await dbContext.Database.MigrateAsync();

        await RoleSeeder.SeedSystemRolesAsync(dbContext);
    }

    app.WebApp.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        };
    });

    app.SetMenu(menu => menu
        .Command("start", "Главное меню"));

    app.UseNavigation<MainMenuScreen>();
    app.MapBotEndpoints();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "StampService Telegram bot terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static void ImportJwtConfigurationFromApiSettings(
    ConfigurationManager configuration,
    IWebHostEnvironment environment)
{
    var apiSettingsPath = Path.GetFullPath(Path.Combine(
        environment.ContentRootPath,
        "..",
        "StampService.API",
        "appsettings.json"));
    var apiEnvironmentSettingsPath = Path.GetFullPath(Path.Combine(
        environment.ContentRootPath,
        "..",
        "StampService.API",
        $"appsettings.{environment.EnvironmentName}.json"));

    var apiConfiguration = new ConfigurationBuilder()
        .AddJsonFile(apiSettingsPath, optional: true, reloadOnChange: false)
        .AddJsonFile(apiEnvironmentSettingsPath, optional: true, reloadOnChange: false)
        .Build();

    foreach (var item in apiConfiguration.GetSection("Jwt").GetChildren())
    {
        if (!string.IsNullOrWhiteSpace(item.Value))
            configuration[$"Jwt:{item.Key}"] = item.Value;
    }
}

public partial class Program;

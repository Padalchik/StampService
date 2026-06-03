using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using StampService.API.Extensions;
using StampService.API.Middlewares;
using StampService.Application;
using StampService.Application.Administration;
using StampService.Application.Services;
using StampService.Infrastructure;
using StampService.Infrastructure.Seeding;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var seedDemo = args.Any(arg => string.Equals(arg, "--seed-demo", StringComparison.OrdinalIgnoreCase));
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    if (seedDemo)
    {
        var botSettingsPath = Path.GetFullPath(Path.Combine(
            builder.Environment.ContentRootPath,
            "..",
            "StampService.TelegramBot",
            "appsettings.json"));
        var botEnvironmentSettingsPath = Path.GetFullPath(Path.Combine(
            builder.Environment.ContentRootPath,
            "..",
            "StampService.TelegramBot",
            $"appsettings.{builder.Environment.EnvironmentName}.json"));

        builder.Configuration.AddJsonFile(botSettingsPath, optional: true, reloadOnChange: false);
        builder.Configuration.AddJsonFile(botEnvironmentSettingsPath, optional: true, reloadOnChange: false);
    }
    else if (IsAdminConfigurationMissing(builder.Configuration))
    {
        ImportAdminTelegramUserIdsFromBotSettings(builder.Configuration, builder.Environment);
    }

    builder.Services.AddControllers();
    builder.Services.AddApiOpenApi();
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection("Admin"));
    builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection("Telegram"));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    await using (var scope = app.Services.CreateAsyncScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (app.Environment.IsDevelopment())
            await dbContext.Database.MigrateAsync();

        if (seedDemo)
        {
            var adminTelegramUserIds = app.Configuration
                .GetSection("Admin:TelegramUserIds")
                .GetChildren()
                .Select(item => long.TryParse(item.Value, out var value) ? value : 0)
                .Where(value => value > 0)
                .ToArray();
            if (adminTelegramUserIds.Length != 1)
            {
                throw new InvalidOperationException(
                    "Для заполнения демо-данными должен быть задан ровно один Admin:TelegramUserIds.");
            }

            await DemoDataSeeder.SeedAsync(dbContext, adminTelegramUserIds[0]);
            return;
        }

        await RoleSeeder.SeedSystemRolesAsync(dbContext);
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        };
    });

    app.UseCustomExceptionHandling();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "StampService API v1");
        });
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "StampService API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static bool IsAdminConfigurationMissing(IConfiguration configuration)
{
    return !configuration
        .GetSection("Admin:TelegramUserIds")
        .GetChildren()
        .Any(item => long.TryParse(item.Value, out var value) && value > 0);
}

static void ImportAdminTelegramUserIdsFromBotSettings(
    ConfigurationManager configuration,
    IWebHostEnvironment environment)
{
    var botSettingsPath = Path.GetFullPath(Path.Combine(
        environment.ContentRootPath,
        "..",
        "StampService.TelegramBot",
        "appsettings.json"));
    var botEnvironmentSettingsPath = Path.GetFullPath(Path.Combine(
        environment.ContentRootPath,
        "..",
        "StampService.TelegramBot",
        $"appsettings.{environment.EnvironmentName}.json"));

    var botConfiguration = new ConfigurationBuilder()
        .AddJsonFile(botSettingsPath, optional: true, reloadOnChange: false)
        .AddJsonFile(botEnvironmentSettingsPath, optional: true, reloadOnChange: false)
        .Build();

    var adminTelegramUserIds = botConfiguration
        .GetSection("Admin:TelegramUserIds")
        .GetChildren()
        .Select(item => item.Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .ToArray();

    for (var index = 0; index < adminTelegramUserIds.Length; index++)
        configuration[$"Admin:TelegramUserIds:{index}"] = adminTelegramUserIds[index];
}

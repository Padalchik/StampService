using Microsoft.EntityFrameworkCore;
using StampService.API.Middlewares;
using StampService.API.Extensions;
using StampService.Application;
using StampService.Application.Services;
using StampService.Infrastructure;
using StampService.Infrastructure.Seeding;

var seedDemo = args.Any(arg => string.Equals(arg, "--seed-demo", StringComparison.OrdinalIgnoreCase));
var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddControllers();
builder.Services.AddApiOpenApi();
builder.Services.AddJwtAuthentication(builder.Configuration);
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

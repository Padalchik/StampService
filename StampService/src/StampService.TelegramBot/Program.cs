using Microsoft.EntityFrameworkCore;
using StampService.Application;
using StampService.Infrastructure;
using StampService.Infrastructure.Seeding;
using StampService.TelegramBot.Features.MainMenu.Screens;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Extensions;
using TelegramBotFlow.Core.Hosting;

var builder = BotApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    $"appsettings.{builder.WebAppBuilder.Environment.EnvironmentName}.json",
    optional: true,
    reloadOnChange: true);

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

app.SetMenu(menu => menu
    .Command("start", "Главное меню"));

app.UseNavigation<MainMenuScreen>();
app.MapBotEndpoints();

await app.RunAsync();

public partial class Program;

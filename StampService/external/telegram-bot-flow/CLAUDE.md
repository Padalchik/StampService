# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

TelegramBotFlow is a .NET 10 framework for building Telegram bots with a middleware pipeline, screen-based navigation, wizard FSM, sessions, and ASP.NET Core Minimal API-style route registration.

## Project Structure

| Package | Responsibility |
|---|---|
| `TelegramBotFlow.Core.Abstractions` | Contracts layer: interfaces (`IScreen`, `IBotAction`, `IBotUser`, `IUpdateMiddleware`, `IEndpointResult`), `UpdateContext`, `ScreenView` builder, `StepResult`, session/navigation state, UI builders. No implementation dependencies. |
| `TelegramBotFlow.Core` | Runtime: middleware pipeline, routing, `BotApplication` host, `ScreenManager`, `WizardMiddleware`, `SessionMiddleware`, in-memory stores, update processing (polling/webhook). |
| `TelegramBotFlow.Core.Redis` | Optional Redis session store (`RedisSessionStore` replaces `InMemorySessionStore`). |
| `TelegramBotFlow.Data.Postgres` | Optional EF Core/PostgreSQL data layer: `BotUser` entity, `BotDbContext<TUser>`, `EfBotUserStore<TUser>`. RootNamespace: `TelegramBotFlow.Core.Data`. |
| `TelegramBotFlow.App` | Example bot application using the framework. |

Tests:
- `tests/TelegramBotFlow.Core.Tests` -- unit tests
- `tests/TelegramBotFlow.IntegrationTests` -- integration tests (Redis via Testcontainers)

## Feature Folder Convention

Каждая фича в `App/Features/{FeatureName}/` (и в любом сервисе, использующем TBF) разбита на роль-папки. Цель — чётко отделить **рендер UI** от **бизнес-логики** и **роутинга TBF pipeline'а**:

| Папка | Что лежит | Тип | Когда заводится |
|---|---|---|---|
| `Screens/` | `IScreen` impls — только рендер `ScreenView` | UI | у фичи есть экраны (≥1 IScreen) |
| `Endpoints/` | `IBotEndpoint` impls + `IBotAction` маркеры — TBF route mappings | Routing | у фичи есть commands / callback actions / deep-links |
| `Handlers/` | Scoped DI services с реальной логикой; вызываются из endpoint'ов через DI-параметр | Business | endpoint вырастает за 20 строк или нужна тестируемость без `BotApplication` |
| `Wizards/` | `BotWizard<TState>` impls + State-классы (`{Name}State.cs` + `{Name}Wizard.cs` рядом) | Multi-step | многошаговый FSM флоу |
| `Middleware/` | `IUpdateMiddleware` impls (фича-специфичные) | Pipeline | редко |
| `Services/` | `IHostedService`, periodic jobs, доменные services | Background | startup/scheduled jobs или сервисы, переиспользуемые между endpoint'ами |

Namespace всегда зеркалит путь до файла: `TelegramBotFlow.App.Features.{Feature}.Screens`, `...Features.{Feature}.Endpoints` и т.д. — это согласуется с уже существующей `UseCases/` конвенцией для HTTP IEndpoint'ов в сервисах.

**Endpoint vs Handler convention.** `IBotEndpoint` — только маппинг команды/deep-link/callback на TBF pipeline (короткий файл, никакой бизнес-логики). Тяжёлая логика выносится в обычный класс-handler со scoped DI-регистрацией — handler инъектится как параметр TBF-делегата и тестируется юнит-тестами без поднятия `BotApplication`.

```
Features/
  Onboarding/
    Endpoints/
      ProfileSetupEndpoint.cs    # StartProfileSetupAction + IBotEndpoint
    Wizards/
      ProfileSetupState.cs       # public class ProfileSetupState
      ProfileSetupWizard.cs      # BotWizard<ProfileSetupState>
    Screens/
      ProfileSetupResultScreen.cs
```

Все три роли auto-discovery'тся стандартными extension-методами: `AddScreens(typeof(Program).Assembly)`, `AddBotEndpoints(...)`, `AddWizards(...)`. Папки сами по себе не влияют на DI.

## Build & Test Commands

```bash
dotnet build TelegramBotFlow.slnx
dotnet test TelegramBotFlow.slnx
dotnet test TelegramBotFlow.slnx --filter "FullyQualifiedName~TestClassName"

# Run the example app
dotnet run --project src/TelegramBotFlow.App

# EF Core migration (Data.Postgres)
dotnet ef migrations add {Name} \
  --project src/TelegramBotFlow.Data.Postgres \
  --startup-project src/TelegramBotFlow.App
```

## Architecture

```
Telegram Update
  -> Channel<Update> (bounded, configurable via UpdateChannelCapacity)
  -> UpdateProcessingWorker (scoped DI per update)
  -> Pipeline: Middleware chain -> Router -> Handler -> IEndpointResult
  -> IEndpointResult.ExecuteAsync(BotExecutionContext)
  -> ScreenManager / NavigationService / IUpdateResponder
```

**Middleware is registered on `BotApplicationBuilder`, not `BotApplication`:**
```csharp
var builder = BotApplication.CreateBuilder(args);
builder.UseErrorHandling();
builder.UseSession();
builder.UseWizards();
// ... other middleware ...

var app = builder.Build();
// app is used only for route registration (MapCommand, MapAction, etc.)
app.Run();
```

**Two runtime modes:**
- **Polling** -- `PollingService` (HostedService) polls `getUpdates` and writes to the channel
- **Webhook** -- ASP.NET Core POST endpoint receives updates, validates `X-Telegram-Bot-Api-Secret-Token`

**Pipeline flow:** Each `UpdateContext` passes through the middleware chain (error handling -> logging -> private chat filter -> session -> access policy -> wizards -> user tracking -> pending input) and terminates at the `UpdateRouter`, which matches the update to a registered handler. Handlers return `IEndpointResult` (analogous to ASP.NET Core `IResult`), which executes itself via `BotExecutionContext`. `UpdateContext.User` (set by `UserTrackingMiddleware`) and `UpdateContext.HandlerName` (set by router) are available for downstream use and structured error logging. Typed input properties (`Photos`, `Document`, `Contact`, `Location`, `Voice`, `VideoNote`, `Video`, `HasMedia`) are available on `UpdateContext`.

## Key Patterns

### Adding a command

Implement `IBotEndpoint` and register with `MapCommand()`:

```csharp
public class StartEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapCommand("start", (UpdateContext ctx) =>
        {
            ctx.Session?.Clear();
            return Task.FromResult(BotResults.NavigateToRoot<MainMenuScreen>());
        });
    }
}
```

### Adding a screen

Implement `IScreen`, return `ScreenView`:

```csharp
public class SettingsScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx) =>
        ValueTask.FromResult(
            new ScreenView("Settings")
                .Button<ChangeLanguageAction>("Language")
                .Row()
                .BackButton());
}
```

Reply keyboard support:

```csharp
new ScreenView("Choose an option:")
    .WithReplyKeyboard(new[] { "Option A", "Option B" })
    .BackButton();

// To remove reply keyboard on next screen:
new ScreenView("Done.").RemoveReplyKeyboard();
```

### Adding a callback action

Create a struct implementing `IBotAction`, register with `MapAction<T>()`:

```csharp
public struct ChangeLanguageAction : IBotAction;

// In endpoint:
app.MapAction<ChangeLanguageAction>((UpdateContext ctx) =>
    Task.FromResult(BotResults.NavigateTo<LanguageScreen>()));

// With payload:
app.MapAction<SelectItemAction, Guid>((UpdateContext ctx, Guid itemId) =>
    Task.FromResult(BotResults.NavigateTo<ItemScreen>()
        .WithArg("itemId", itemId)));
```

### Adding input handling

Use `AwaitInput<T>()` on ScreenView and `MapInput<T>()` for the handler:

```csharp
// In screen:
new ScreenView("Enter your name:")
    .AwaitInput<NameInputAction>()
    .BackButton();

// In endpoint:
app.MapInput<NameInputAction>((UpdateContext ctx) =>
{
    string name = ctx.MessageText!;
    // process name...
    return Task.FromResult(BotResults.Back("Saved!"));
});
```

### Adding a wizard

Extend `BotWizard<TState>`, implement `ConfigureSteps()` and `OnFinishedAsync()`:

```csharp
public class CreateItemWizard : BotWizard<CreateItemState>
{
    protected override void ConfigureSteps(WizardBuilder<CreateItemState> builder)
    {
        builder
            .Step("name",
                (ctx, state) => new ScreenView("Enter item name:"),
                (ctx, state) =>
                {
                    state.Name = ctx.MessageText!;
                    return StepResult.GoTo("confirm");
                })
            .Step("confirm",
                (ctx, state) => new ScreenView($"Create '{state.Name}'?")
                    .Button("Yes", "wizard:confirm")
                    .Button("No", "wizard:cancel"),
                (ctx, state) => ctx.CallbackData switch
                {
                    "wizard:confirm" => StepResult.Finish(),
                    _ => StepResult.GoBack()
                });
    }

    public override async Task<IEndpointResult> OnFinishedAsync(
        UpdateContext context, CreateItemState state)
    {
        // persist state.Name...
        return BotResults.NavigateToRoot<MainMenuScreen>();
    }

    // Optional: handle cancellation
    public override Task<IEndpointResult> OnCancelledAsync(
        UpdateContext context, CreateItemState state)
    {
        return Task.FromResult(BotResults.Back("Cancelled."));
    }
}
```

Launch: `BotResults.StartWizard<CreateItemWizard>()`

### Adding custom middleware

Implement `IUpdateMiddleware`, register with `Use<T>()` on the **builder**:

```csharp
public class RateLimitMiddleware : IUpdateMiddleware
{
    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        // pre-processing
        await next(context);
        // post-processing
    }
}

// Registration (on builder, before Build()):
builder.Use<RateLimitMiddleware>();

// Conditional middleware:
builder.UseWhen<RateLimitMiddleware>(ctx => !ctx.IsAdmin);
```

### Proactive messaging (IBotNotifier)

Send messages outside the update pipeline:

```csharp
public class MyService(IBotNotifier notifier)
{
    public async Task NotifyUser(long chatId)
    {
        await notifier.SendTextAsync(chatId, "Hello!");
    }
}
```

### Batch messaging (IBotBroadcaster)

Send messages to many users with error tracking and blocked user detection:

```csharp
public class MyService(IBotBroadcaster broadcaster)
{
    public async Task NotifyAll(IEnumerable<long> chatIds)
    {
        var result = await broadcaster.BroadcastTextAsync(chatIds, "Update available!");
        // result.Succeeded, result.Failed, result.Blocked
    }
}
```

### Chat administration (IChatAdministrationApi)

Admin-операции над чатами/каналами: resolve, проверка прав/членства, invite-link CRUD,
approve/decline `chat_join_request`, kick. Все методы возвращают `ChatApiResult<T>` —
никаких exception'ов на бизнес-фейлы (минимальный Result-тип в `Core.Abstractions`,
чтобы не тащить CSharpFunctionalExtensions в contracts).

```csharp
public class CourseChatBinder(IChatAdministrationApi chatApi)
{
    public async Task<string?> BindChatAsync(long chatId)
    {
        ChatApiResult<BotChatPermissions> perms = await chatApi.GetBotPermissionsAsync(chatId);
        if (perms.IsFailure || !perms.Value!.IsAdministrator) return null;

        ChatApiResult<string> link = await chatApi.CreateJoinRequestInviteLinkAsync(
            chatId, name: "course-XXX");

        return link.IsSuccess ? link.Value : null;
    }
}

// Approve/decline join request handler:
app.MapUpdate(
    ctx => ctx.Update.Type == UpdateType.ChatJoinRequest,
    async (UpdateContext ctx, IChatAdministrationApi chatApi) =>
    {
        var req = ctx.Update.ChatJoinRequest!;
        ChatApiResult<ChatMemberInfo> member = await chatApi.GetChatMemberAsync(req.Chat.Id, req.From.Id);
        if (member.IsSuccess && member.Value!.IsActiveMember)
            await chatApi.ApproveChatJoinRequestAsync(req.Chat.Id, req.From.Id);
        else
            await chatApi.DeclineChatJoinRequestAsync(req.Chat.Id, req.From.Id);
        return BotResults.Empty();
    });
```

`ChatMembership` enum: `NOT_MEMBER | MEMBER | ADMINISTRATOR | CREATOR | RESTRICTED_BUT_MEMBER`.
`ChatApiErrorCode` enum: `None | ChatNotReachable | RateLimited | Other`.

`KickChatMemberAsync` = ban + unban сразу — выкинуть юзера, но позволить ему снова вступить через invite link. Permanent ban делается через прямой вызов `Telegram.Bot.ITelegramBotClient.BanChatMember`.

### Deep link routing

Route `/start payload` deep links with HIGH priority:

```csharp
app.MapDeepLink("referral", (UpdateContext ctx, string payload) =>
    Task.FromResult(BotResults.NavigateTo<ReferralScreen>()
        .WithArg("code", payload)));
```

### Chat member updates

Route MyChatMember updates (auto-blocks on Kicked status):

```csharp
app.MapChatMember((UpdateContext ctx) =>
    Task.FromResult(BotResults.Empty()));
```

### DI registration

```csharp
// Required: core framework
builder.Services.AddTelegramBotFlow(builder.Configuration);

// Optional: screens from assembly
builder.Services.AddScreens(typeof(Program).Assembly);

// Optional: wizards from assembly
builder.Services.AddWizards(typeof(Program).Assembly);

// Optional: IBotEndpoint implementations
builder.Services.AddBotEndpoints(typeof(Program).Assembly);

// Optional: PostgreSQL user storage
builder.Services.AddBotCoreData(builder.Configuration);

// Optional: Redis sessions (replaces InMemorySessionStore)
builder.Services.AddRedisSessionStore(builder.Configuration);

// Optional: customize framework strings (back button text, error message, etc.)
builder.Services.Configure<BotMessages>(m =>
{
    m.ErrorMessage = "Something went wrong.";
    m.BackButton = "Back";
});
```

`AddTelegramBotFlow` registers `IBotNotifier`, `IBotBroadcaster`, and `IChatAdministrationApi` automatically.

## Configuration

`BotConfiguration` is bound from the `"Bot"` section of `appsettings.json`:

| Property | Type | Default | Description |
|---|---|---|---|
| `Token` | `string` | required | Telegram Bot API token |
| `Mode` | `BotMode` | `POLLING` | `POLLING` or `WEBHOOK` |
| `WebhookUrl` | `string?` | `null` | Public URL for webhook mode |
| `WebhookPath` | `string` | `/api/bot/webhook` | Webhook endpoint path |
| `WebhookSecretToken` | `string?` | `null` | Secret for `X-Telegram-Bot-Api-Secret-Token` validation |
| `AdminUserIds` | `long[]` | `[]` | Telegram user IDs with admin access |
| `StorageChannelId` | `long` | `0` | Channel ID for file storage |
| `AllowedUpdates` | `UpdateType[]` | `[Message, CallbackQuery, MyChatMember]` | Update types to receive |
| `PayloadCacheSize` | `int` | `500` | LRU cache size for large callback payloads |
| `SessionLockTimeoutSeconds` | `int` | `30` | Timeout for per-user session lock |
| `MaxConcurrentUpdates` | `int` | `100` | Max concurrent update processing tasks |
| `MaxNavigationDepth` | `int` | `20` | Max screen navigation stack depth |
| `UpdateChannelCapacity` | `int` | `1000` | Bounded channel capacity for updates |
| `WizardDefaultTtlMinutes` | `int` | `60` | Default wizard state TTL |
| `ShutdownTimeoutSeconds` | `int` | `30` | Graceful shutdown timeout |
| `TelegramRateLimitPerSecond` | `int` | `30` | Token bucket rate limiter for Telegram API |
| `MaxRetryOnRateLimit` | `int` | `3` | Max retries on 429 responses |
| `HealthCheckPath` | `string` | `/health` | Health check endpoint path |

`BotMessages` -- configurable framework strings (override via `Configure<BotMessages>()`):

| Property | Default | Description |
|---|---|---|
| `BackButton` | `"Back"` | Back button text |
| `MenuButton` | `"Menu"` | Menu button text |
| `CloseButton` | `"Close"` | Close button text |
| `PayloadExpired` | `"Action expired..."` | Shown when payload LRU entry is evicted |
| `ErrorMessage` | `"An error occurred..."` | Message sent on unhandled errors |

Redis session config is in the `"Redis"` section:

| Property | Type | Default | Description |
|---|---|---|---|
| `ConnectionString` | `string` | `localhost:6379` | Redis connection string |
| `SessionTtlMinutes` | `int?` | `null` | Session TTL in minutes (null = no expiry) |

## Known Gotchas

- **Middleware registration on builder** -- All `Use*()` calls (e.g. `UseErrorHandling()`, `UseSession()`, `UseWizards()`) are on `BotApplicationBuilder`, not `BotApplication`. Called before `Build()`. `BotApplication` is only for route registration (`MapCommand`, `MapAction`, etc.).

- **Middleware ordering** -- `UseWizards()` and `UsePendingInput()` must be registered after `UseSession()`. This is validated at startup via `MiddlewareOrderValidator`; incorrect order throws `InvalidOperationException`. The `UseErrorHandling()` middleware should be first.

- **Payload encoding** -- Button payloads < 64 bytes UTF-8 are inlined in callback_data (`action:j:{json}`). Payloads >= 64 bytes are stored in session LRU cache and referenced by shortId (`action:s:{shortId}`). The LRU cache holds `PayloadCacheSize` entries (default 500). Expired payloads throw `PayloadExpiredException`.

- **Telegram callback_data 64-byte limit** -- Total callback_data must fit in 64 bytes. Action IDs + payload prefix consume part of this budget. Keep action IDs short.

- **Session lock scope** -- `SessionMiddleware` acquires a per-user lock for the entire pipeline execution (try-finally pattern). Lock timeout is configurable via `SessionLockTimeoutSeconds`. This prevents concurrent updates from corrupting session state.

- **Screen ID convention** -- `ClassName` is converted to `snake_case` with `Screen` suffix stripped: `MainMenuScreen` -> `main_menu`. Override with `[ScreenId("custom_id")]` attribute.

- **Action ID** -- Defaults to the type name (`struct ChangeLanguage : IBotAction` -> `"ChangeLanguage"`). Override with `[ActionId("custom_id")]` attribute.

- **NavigationStack is read-only** -- The `NavigationStack` property returns `IReadOnlyList<string>`. Mutate only through framework methods (`BotResults.Back()`, `BotResults.NavigateTo<T>()`).

- **UserTrackingMiddleware memory** -- Uses `MemoryCache` with 1-hour sliding expiration to avoid DB lookups on every update. Not unbounded.

- **Data.Postgres RootNamespace** -- The `TelegramBotFlow.Data.Postgres` project uses `RootNamespace = TelegramBotFlow.Core.Data`. Import `TelegramBotFlow.Core.Data`, not `TelegramBotFlow.Data.Postgres`.

- **InternalsVisibleTo** -- `Core.Abstractions` exposes internals to `Core`, `Core.Redis`, and test projects. `Core` exposes internals to test projects. This enables testing of internal pipeline components.

- **Wizard state deserialization** -- If `PayloadJson` in `WizardStorageState` is malformed, the wizard catches `JsonException` and returns `BotResults.Back()` (exits the wizard gracefully).

- **OnEnter exception safety** -- If a wizard step's `OnEnter` throws, the state is rolled back to pre-`OnEnter` snapshot and the user stays on the current step.

- **PendingInputMiddleware fallthrough** -- If `PendingInputActionId` is set but no handler is registered for that action ID, the middleware logs a warning and passes the update to the router (does not silently swallow it).

- **Commands reset pending input** -- If a user sends a `/command` while input is pending, the pending state is cleared and the command is routed normally.

- **MyChatMember in AllowedUpdates** -- `AllowedUpdates` now includes `MyChatMember` by default. Use `MapChatMember()` to handle these updates. Kicked status auto-blocks the user.

- **Rate limiter** -- Telegram API calls go through a `TokenBucketRateLimiter` (configurable via `TelegramRateLimitPerSecond`). HTTP retry with exponential backoff handles 429/500/503 responses (configurable via `MaxRetryOnRateLimit`).

- **InMemoryWizardStore uses IMemoryCache** -- Wizard states are stored in `IMemoryCache` with TTL (`WizardDefaultTtlMinutes`), preventing memory leaks from abandoned wizard sessions.

- **UpdateContext.User** -- Set by `UserTrackingMiddleware`. May be `null` if middleware is not registered or user is new. Always check for null.

- **UpdateContext.HandlerName** -- Set by the router for structured error logging. Available in error handling middleware.

- **Wizard cancellation** -- Override `OnCancelledAsync()` in `BotWizard<TState>` to handle user cancellation (e.g. cleanup resources).

- **ErrorMessage moved to BotMessages** -- `BotConfiguration.ErrorMessage` was removed. Configure error message text via `Configure<BotMessages>()`.

- **Health check endpoint** -- Exposed at `HealthCheckPath` (default `/health`). Useful for container orchestration liveness probes.

- **Graceful shutdown** -- Configurable via `ShutdownTimeoutSeconds`. The framework waits for in-flight updates to complete before shutting down.

## Dependency Graph

```
Core.Abstractions  (contracts only, Telegram.Bot + DI.Abstractions)
     |
     +----> Core  (runtime, ASP.NET Core, Telegram.Bot)
     |       |
     |       +----> Data.Postgres  (EF Core, Npgsql)
     |       |
     |       +----> App  (example bot)
     |
     +----> Core.Redis  (StackExchange.Redis)
```

## Post-Change Checklist

- `dotnet build TelegramBotFlow.slnx` passes (warnings are errors)
- `dotnet test TelegramBotFlow.slnx` passes (Docker must be running for integration tests)
- If adding public API: add XML doc comments (`GenerateDocumentationFile` is enabled)
- If changing middleware: verify ordering rules in `MiddlewareOrderValidator`
- If changing session/navigation state: check both `InMemorySessionStore` and `RedisSessionStore`
- If changing `IBotUserStore`: update both `EfBotUserStore` and `UserTrackingMiddleware`
- If modifying `Core.Abstractions` contracts: check `InternalsVisibleTo` consumers (Core, Core.Redis, tests)
- Never delete or modify existing EF migrations in `Data.Postgres`

## Coding Style

- `.editorconfig`: 4 spaces, nullable enabled, `TreatWarningsAsErrors=true`
- `PascalCase` for types/methods/properties, `camelCase` for locals/params, `_camelCase` for private fields
- Constants and enum values: `SCREAMING_CASE` (intentional)
- Prefer typed bot actions (`IBotAction` structs) over string callback IDs
- Feature code co-located under `Features/{FeatureName}/` in the App project

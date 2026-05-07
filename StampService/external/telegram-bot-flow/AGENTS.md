# Repository Guidelines

## Project Structure & Module Organization

- `src/` contains production code split by project.
- `src/TelegramBotFlow.App` is the example bot application entry point and feature code (`Features/{FeatureName}/`).
- `src/TelegramBotFlow.Core.Abstractions` contains contracts and interfaces: `IScreen`, `IBotAction`, `IBotUser`, `IBotUserStore`, `IUpdateMiddleware`, `IEndpointResult`, `IBotNotifier`, `IBotBroadcaster`, `UpdateContext`, `ScreenView` builder, `StepResult`, `BotMessages`, session/navigation state, UI builders. No implementation dependencies.
- `src/TelegramBotFlow.Core` contains runtime: middleware pipeline, routing (`UpdateRouter`), `BotApplication` host, `BotApplicationBuilder` (middleware registration), `ScreenManager`, `WizardMiddleware`, `SessionMiddleware`, `ConditionalMiddleware`, `TelegramRateLimitHandler`, in-memory stores, update processing (polling/webhook).
- `src/TelegramBotFlow.Core.Redis` contains Redis session store (`RedisSessionStore` replaces `InMemorySessionStore`).
- `src/TelegramBotFlow.Data.Postgres` contains EF Core/PostgreSQL data layer: `BotUser` entity, `BotDbContext<TUser>`, `EfBotUserStore<TUser>`. RootNamespace: `TelegramBotFlow.Core.Data`.
- `tests/TelegramBotFlow.Core.Tests` holds unit tests; `tests/TelegramBotFlow.IntegrationTests` holds integration tests (including Redis/Testcontainers).

## Dependency Graph

```
Core.Abstractions  (contracts: Telegram.Bot, DI.Abstractions)
     |
     +----> Core  (runtime: ASP.NET Core, Telegram.Bot)
     |       |
     |       +----> Data.Postgres  (EF Core, Npgsql)
     |       |
     |       +----> App  (example bot)
     |
     +----> Core.Redis  (StackExchange.Redis)
```

## Build, Test, and Development Commands

- `dotnet build TelegramBotFlow.slnx` builds the full solution (`TreatWarningsAsErrors=true`).
- `dotnet test TelegramBotFlow.slnx` runs all unit and integration tests.
- `dotnet test --filter "FullyQualifiedName~TestClassName"` runs specific tests.
- `dotnet run --project src/TelegramBotFlow.App` runs the bot locally.
- `docker compose -f docker-compose-infra.yml up -d` starts local PostgreSQL + Seq.

## Architecture

```
Telegram Update
  -> Channel<Update> (bounded, configurable via UpdateChannelCapacity)
  -> UpdateProcessingWorker (scoped DI per update, MaxConcurrentUpdates)
  -> Pipeline: Middleware chain -> Router -> Handler -> IEndpointResult
  -> IEndpointResult.ExecuteAsync(BotExecutionContext)
  -> ScreenManager / NavigationService / IUpdateResponder
```

Two runtime modes: **Polling** (HostedService polls getUpdates) and **Webhook** (ASP.NET Core POST endpoint with secret token validation).

## Component Responsibilities

| Component | Responsibility |
|---|---|
| `BotApplication` | Central API: route mapping (`MapCommand`, `MapAction`, `MapInput`, `MapDeepLink`, `MapChatMember`), menu setup, pipeline build + run |
| `BotApplicationBuilder` | Creates `WebApplicationBuilder`, auto-discovers endpoints/screens/wizards from entry assembly, middleware registration (`UseSession()`, `UseWizards()`, etc.) |
| `UpdatePipeline` | Builds and executes the middleware delegate chain |
| `UpdateRouter` | Matches updates to registered handlers (commands, callbacks, messages, fallback) |
| `ScreenManager` | Renders screens, builds keyboard with payload encoding, manages nav messages |
| `NavigationService` | Stack-based screen navigation (push, pop, root, refresh) |
| `SessionMiddleware` | Acquires per-user lock, loads/saves session (try-finally) |
| `WizardMiddleware` | Intercepts updates when a wizard is active, delegates to `IBotWizard` |
| `PendingInputMiddleware` | Routes text messages to registered input handlers when `PendingInputActionId` is set |
| `UserTrackingMiddleware<T>` | Auto-creates user records via `IBotUserStore<T>` with MemoryCache deduplication |
| `ScreenRegistry` | Maps screen IDs to types, respects `[ScreenId]` attribute and snake_case convention |
| `ActionIdResolver` | Resolves action ID from type, respects `[ActionId]` attribute |
| `MiddlewareOrderValidator` | Validates that session-dependent middlewares are registered after `UseSession()` |
| `ConditionalMiddleware` | Wraps a middleware to run only when a predicate matches the `UpdateContext` |
| `MiddlewareBranchBuilder` | Builds conditional middleware branches for the pipeline |
| `InputHandlerRegistry` | Stores registered input handlers by action ID |
| `WizardRegistry` | Stores registered wizard types by name |
| `InMemoryWizardStore` | Stores active wizard state using `IMemoryCache` |
| `IBotNotifier` | Sends one-off messages to a specific chat outside the normal pipeline |
| `IBotBroadcaster` | Sends messages to multiple chats with rate-limit awareness |
| `TelegramRateLimitHandler` | Handles Telegram API 429 responses with automatic retry and backoff |
| `BotMessages` | Centralised localizable strings (BackButton, MenuButton, CloseButton, PayloadExpired, ErrorMessage) |
| `BotRuntime` | Configures webhook/polling, applies menu, starts ASP.NET Core host |

## UpdateContext Properties

`UpdateContext` carries the current update through the pipeline. v2.0 adds: `User` (`IBotUser?`), `HandlerName` (`string?`), `Photos`, `Document`, `Contact`, `Location`, `Voice`, `VideoNote`, `Video`, `HasMedia`.

## Extension Points

### IBotUserStore<TUser>
Persistence abstraction for bot users. Built-in: `EfBotUserStore<TUser>` (Data.Postgres). Implement for custom storage (MongoDB, in-memory, etc.).

### Custom Middleware (IUpdateMiddleware)
Implement `IUpdateMiddleware.InvokeAsync(UpdateContext, UpdateDelegate)`, register with `builder.Use<T>()`. Resolved from DI per-update. Use `ConditionalMiddleware` / `MiddlewareBranchBuilder` for conditional pipeline branches.

### Screens (IScreen)
Implement `IScreen.RenderAsync(UpdateContext)`, return `ScreenView`. Auto-discovered from assembly via `AddScreens()`. Override screen ID with `[ScreenId]` attribute. `ScreenView` supports `WithReplyKeyboard()` / `RemoveReplyKeyboard()` for custom reply keyboards.

### Wizards (BotWizard<TState>)
Extend `BotWizard<TState>`, implement `ConfigureSteps()` and `OnFinishedAsync()`. Optionally override `OnCancelledAsync()` for custom cancellation logic. Auto-discovered from assembly via `AddWizards()`. Steps have render + process phases, optional `OnEnter` hook.

### IBotEndpoint
Implement `IBotEndpoint.MapEndpoint(BotApplication)` for auto-discovered route registration. Discovered via `AddBotEndpoints()` + `MapBotEndpoints()`. v2.0 adds `MapDeepLink()` for `/start` deep-link parameters and `MapChatMember()` for chat member status updates.

### ISessionStore
Replace session storage. Built-in: `InMemorySessionStore` (default), `RedisSessionStore` (Core.Redis). Register with `AddSessionStore<T>()` or `AddRedisSessionStore()`.

### IBotAction
Marker interface for typed callback actions. Use as struct for zero-allocation. Override action ID with `[ActionId]` attribute.

### IBotNotifier
Send one-off messages to a specific chat outside the normal update pipeline.

### IBotBroadcaster
Send messages to multiple chats with built-in rate-limit handling via `TelegramRateLimitHandler`.

### BotMessages
Centralised class for localizable UI strings: `BackButton`, `MenuButton`, `CloseButton`, `PayloadExpired`, `ErrorMessage`. Override to customise or localise bot text.

## Coding Style & Naming Conventions

- Follow `.editorconfig`: C# uses 4 spaces, nullable enabled, and warnings are treated as errors.
- `PascalCase` for types/methods/properties, `camelCase` for locals/params, `_camelCase` for private fields.
- Constants and enum values use `SCREAMING_CASE` (intentional deviation from default .NET style).
- Keep feature files co-located under `Features/{FeatureName}/` and prefer typed bot actions over string callback IDs.

## Testing Guidelines

- Frameworks: xUnit + FluentAssertions + NSubstitute; coverage via `coverlet.collector`.
- Name test files as `*Tests.cs`; keep tests grouped by domain folder (`Routing/`, `Pipeline/`, `Sessions/`, etc.).
- For integration tests using Redis/Testcontainers, ensure Docker is running.
- Run coverage with `dotnet test --collect:"XPlat Code Coverage"` before major PRs.

## Commit & Pull Request Guidelines

- Conventional Commits: `feat:`, `fix:`, `refactor:`, `chore:`, `docs:`, `test:`.
- Keep commits focused and avoid mixing refactors with behavior changes.
- PRs should include clear scope, motivation, and test evidence.

## Security & Configuration Tips

- Never commit bot tokens or real secrets; use `.env`/local settings and keep `.env.example` sanitized.
- Bot configuration is in the `"Bot"` section of `appsettings.json`. v2.0 adds: `PayloadCacheSize`, `SessionLockTimeoutSeconds`, `MaxConcurrentUpdates`, `MaxNavigationDepth`, `UpdateChannelCapacity`, `WizardDefaultTtlMinutes`, `ShutdownTimeoutSeconds`, `TelegramRateLimitPerSecond`, `MaxRetryOnRateLimit`, `HealthCheckPath`.
- `AllowedUpdates` now includes `MyChatMember` by default.
- Redis configuration is in the `"Redis"` section.
- Database connection string is in `ConnectionStrings:Database`.

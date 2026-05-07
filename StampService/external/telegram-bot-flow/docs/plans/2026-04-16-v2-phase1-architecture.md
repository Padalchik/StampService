# Phase 1: Architecture Refactoring — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split BotApplication into Builder (config/middleware) + App (routes/run), extract hardcoded values into configuration, add BotMessages localization, add typed properties to UpdateContext.

**Architecture:** BotApplicationBuilder owns middleware registration and service configuration. BotApplication is immutable after Build(), owns route registration and RunAsync(). All magic numbers move to BotConfiguration. Hardcoded strings move to BotMessages options class.

**Tech Stack:** .NET 10, ASP.NET Core, Telegram.Bot 22.9.0, xUnit, FluentAssertions, NSubstitute

**Spec:** `docs/specs/2026-04-16-v2-foundation-design.md` sections 1.1–1.4

---

## File Map

**New files:**
- `src/TelegramBotFlow.Core.Abstractions/BotMessages.cs` — localized string defaults
- `tests/TelegramBotFlow.Core.Tests/BotMessagesTests.cs` — tests for BotMessages defaults

**Modified files:**
- `src/TelegramBotFlow.Core/Hosting/BotConfiguration.cs` — add configurable parameters
- `src/TelegramBotFlow.Core.Abstractions/Context/UpdateContext.cs` — add User, HandlerName properties
- `src/TelegramBotFlow.Core/Hosting/BotApplicationBuilder.cs` — add middleware registration methods
- `src/TelegramBotFlow.Core/Hosting/BotApplication.cs` — remove middleware methods, keep routes
- `src/TelegramBotFlow.Core.Abstractions/Screens/ScreenView.cs` — use BotMessages for button defaults
- `src/TelegramBotFlow.Core.Abstractions/Exceptions/PayloadExpiredException.cs` — use BotMessages
- `src/TelegramBotFlow.Core.Abstractions/Sessions/NavigationState.cs` — configurable MAX_PAYLOADS
- `src/TelegramBotFlow.Core.Abstractions/Sessions/UserSession.cs` — configurable MAX_NAVIGATION_DEPTH
- `src/TelegramBotFlow.Core/Sessions/InMemorySessionLockProvider.cs` — configurable timeout
- `src/TelegramBotFlow.Core/Hosting/UpdateProcessingWorker.cs` — configurable concurrency
- `src/TelegramBotFlow.Core/Extensions/ServiceCollectionExtensions.cs` — register BotMessages, pass config
- `src/TelegramBotFlow.Core/Pipeline/Middlewares/ErrorHandlingMiddleware.cs` — use BotMessages
- `src/TelegramBotFlow.Core/Pipeline/Middlewares/UserTrackingMiddleware.cs` — set context.User
- `src/TelegramBotFlow.Core/Routing/UpdateRouter.cs` — set context.HandlerName
- `src/TelegramBotFlow.App/Program.cs` — update to new builder/app split API
- `tests/TelegramBotFlow.IntegrationTests/Helpers/BotWebApplicationFactory.cs` — adapt to new API
- `tests/TelegramBotFlow.Core.Tests/Hosting/MiddlewareOrderingTests.cs` — adapt if needed
- Multiple existing test files — verify compilation after breaking changes

---

### Task 1: Add BotMessages class

**Files:**
- Create: `src/TelegramBotFlow.Core.Abstractions/BotMessages.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/BotMessagesTests.cs`

- [ ] **Step 1: Write the test**

```csharp
// tests/TelegramBotFlow.Core.Tests/BotMessagesTests.cs
using FluentAssertions;

namespace TelegramBotFlow.Core.Tests;

public sealed class BotMessagesTests
{
    [Fact]
    public void Defaults_AreNotEmpty()
    {
        var messages = new BotMessages();

        messages.BackButton.Should().NotBeNullOrWhiteSpace();
        messages.MenuButton.Should().NotBeNullOrWhiteSpace();
        messages.CloseButton.Should().NotBeNullOrWhiteSpace();
        messages.PayloadExpired.Should().NotBeNullOrWhiteSpace();
        messages.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Properties_AreOverridable()
    {
        var messages = new BotMessages
        {
            BackButton = "Custom Back",
            ErrorMessage = "Custom Error"
        };

        messages.BackButton.Should().Be("Custom Back");
        messages.ErrorMessage.Should().Be("Custom Error");
    }
}
```

- [ ] **Step 2: Run test — expect compilation failure**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~BotMessagesTests" --no-restore`
Expected: FAIL — `BotMessages` type not found

- [ ] **Step 3: Create BotMessages**

```csharp
// src/TelegramBotFlow.Core.Abstractions/BotMessages.cs
namespace TelegramBotFlow.Core;

/// <summary>
/// Default UI strings used by the framework. Override via Configure&lt;BotMessages&gt;() for localization.
/// </summary>
public class BotMessages
{
    public string BackButton { get; set; } = "\u2190 Back";
    public string MenuButton { get; set; } = "\u2630 Menu";
    public string CloseButton { get; set; } = "\u2190 Back";
    public string PayloadExpired { get; set; } = "Button data expired. Please refresh the menu.";
    public string ErrorMessage { get; set; } = "An error occurred. Please try again later.";
}
```

- [ ] **Step 4: Run test — expect pass**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~BotMessagesTests" --no-restore`
Expected: 2 passed

- [ ] **Step 5: Commit**

```bash
git add src/TelegramBotFlow.Core.Abstractions/BotMessages.cs tests/TelegramBotFlow.Core.Tests/BotMessagesTests.cs
git commit -m "feat: add BotMessages class for localizable framework strings"
```

---

### Task 2: Add configurable parameters to BotConfiguration

**Files:**
- Modify: `src/TelegramBotFlow.Core/Hosting/BotConfiguration.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Hosting/BotConfigurationTests.cs`

- [ ] **Step 1: Write the test**

```csharp
// tests/TelegramBotFlow.Core.Tests/Hosting/BotConfigurationTests.cs
using FluentAssertions;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.Core.Tests.Hosting;

public sealed class BotConfigurationTests
{
    [Fact]
    public void Defaults_HaveSensibleValues()
    {
        var config = new BotConfiguration();

        config.PayloadCacheSize.Should().Be(500);
        config.SessionLockTimeoutSeconds.Should().Be(10);
        config.MaxConcurrentUpdates.Should().Be(100);
        config.MaxNavigationDepth.Should().Be(20);
        config.UpdateChannelCapacity.Should().Be(1000);
        config.WizardDefaultTtlMinutes.Should().Be(60);
        config.ShutdownTimeoutSeconds.Should().Be(30);
        config.TelegramRateLimitPerSecond.Should().Be(25);
        config.MaxRetryOnRateLimit.Should().Be(3);
        config.HealthCheckPath.Should().Be("/health");
    }
}
```

- [ ] **Step 2: Run test — expect compilation failure**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~BotConfigurationTests" --no-restore`
Expected: FAIL — properties not found

- [ ] **Step 3: Add properties to BotConfiguration**

Add the following properties to `src/TelegramBotFlow.Core/Hosting/BotConfiguration.cs` after existing properties (after line ~16):

```csharp
    public int PayloadCacheSize { get; set; } = 500;
    public int SessionLockTimeoutSeconds { get; set; } = 10;
    public int MaxConcurrentUpdates { get; set; } = 100;
    public int MaxNavigationDepth { get; set; } = 20;
    public int UpdateChannelCapacity { get; set; } = 1000;
    public int WizardDefaultTtlMinutes { get; set; } = 60;
    public int ShutdownTimeoutSeconds { get; set; } = 30;
    public int TelegramRateLimitPerSecond { get; set; } = 25;
    public int MaxRetryOnRateLimit { get; set; } = 3;
    public string HealthCheckPath { get; set; } = "/health";
```

Also **remove** `ErrorMessage` property (line 14) — it moves to `BotMessages.ErrorMessage`.

- [ ] **Step 4: Fix ErrorHandlingMiddleware to use BotMessages**

In `src/TelegramBotFlow.Core/Pipeline/Middlewares/ErrorHandlingMiddleware.cs`:
- Change constructor to inject `IOptions<BotMessages>` instead of reading `config.Value.ErrorMessage`
- Use `_messages.ErrorMessage` in the catch block (line ~54)

```csharp
// Replace _errorMessage field initialization:
private readonly string _errorMessage;

// Constructor change:
public ErrorHandlingMiddleware(
    ILogger<ErrorHandlingMiddleware> logger,
    IUpdateResponder responder,
    IOptions<BotMessages> messages)
{
    _logger = logger;
    _responder = responder;
    _errorMessage = messages.Value.ErrorMessage;
}
```

- [ ] **Step 5: Fix compilation — build the solution**

Run: `dotnet build TelegramBotFlow.slnx`
Expected: Fix any remaining references to `BotConfiguration.ErrorMessage`. Check `ServiceCollectionExtensions.cs` — add `services.Configure<BotMessages>(...)` registration.

- [ ] **Step 6: Run all tests**

Run: `dotnet test TelegramBotFlow.slnx`
Expected: All tests pass (including the new BotConfigurationTests)

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: add configurable parameters to BotConfiguration, migrate ErrorMessage to BotMessages"
```

---

### Task 3: Add User and HandlerName to UpdateContext

**Files:**
- Modify: `src/TelegramBotFlow.Core.Abstractions/Context/UpdateContext.cs`
- Modify: `src/TelegramBotFlow.Core/Pipeline/Middlewares/UserTrackingMiddleware.cs`
- Modify: `src/TelegramBotFlow.Core/Routing/UpdateRouter.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Context/UpdateContextPropertiesTests.cs`

- [ ] **Step 1: Write the test**

```csharp
// tests/TelegramBotFlow.Core.Tests/Context/UpdateContextPropertiesTests.cs
using FluentAssertions;
using TelegramBotFlow.Core.Users;

namespace TelegramBotFlow.Core.Tests.Context;

public sealed class UpdateContextPropertiesTests
{
    [Fact]
    public void User_DefaultsToNull()
    {
        var ctx = TestHelpers.CreateMessageContext("hello");
        ctx.User.Should().BeNull();
    }

    [Fact]
    public void HandlerName_DefaultsToNull()
    {
        var ctx = TestHelpers.CreateMessageContext("hello");
        ctx.HandlerName.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run test — expect compilation failure**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~UpdateContextPropertiesTests" --no-restore`
Expected: FAIL — `User` and `HandlerName` properties not found

- [ ] **Step 3: Add properties to UpdateContext**

In `src/TelegramBotFlow.Core.Abstractions/Context/UpdateContext.cs`, add after the `IsAdmin` property (~line 35):

```csharp
    /// <summary>
    /// Current bot user, set by UserTrackingMiddleware. Null if middleware not registered.
    /// </summary>
    public IBotUser? User { get; internal set; }

    /// <summary>
    /// Name of the matched handler, set by UpdateRouter at dispatch time. For structured logging.
    /// </summary>
    public string? HandlerName { get; internal set; }
```

- [ ] **Step 4: Set User in UserTrackingMiddleware**

In `src/TelegramBotFlow.Core/Pipeline/Middlewares/UserTrackingMiddleware.cs`, after the user is found/created and cached (~line 39), add:

```csharp
    context.User = user;
```

Where `user` is the `TUser` returned from store or newly created.

- [ ] **Step 5: Set HandlerName in UpdateRouter**

In `src/TelegramBotFlow.Core/Routing/UpdateRouter.cs`, in the `BuildTerminal()` method, inside the route match loop, before calling `route.Handler(context)`, add:

```csharp
    context.HandlerName = route.Pattern ?? route.Type.ToString();
```

- [ ] **Step 6: Run tests**

Run: `dotnet test TelegramBotFlow.slnx`
Expected: All pass

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: add User and HandlerName typed properties to UpdateContext"
```

---

### Task 4: Wire ScreenView and PayloadExpiredException to BotMessages

**Files:**
- Modify: `src/TelegramBotFlow.Core.Abstractions/Screens/ScreenView.cs`
- Modify: `src/TelegramBotFlow.Core.Abstractions/Exceptions/PayloadExpiredException.cs`
- Modify: `src/TelegramBotFlow.Core/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Change ScreenView default button text parameters**

In `src/TelegramBotFlow.Core.Abstractions/Screens/ScreenView.cs`:

Line 180 — `BackButton`: Change default from hardcoded Russian to `null`, resolve from DI at render time. However, `ScreenView` is a value object created in user code — it doesn't have DI access.

**Better approach:** Keep `string text` parameter on `BackButton`/`MenuButton`/`CloseButton` but change the defaults to English. The developer passes their own text when they want localization. This is consistent with the current API (explicit text parameter).

```csharp
// Line 180: Change default
public ScreenView BackButton(string text = "\u2190 Back")

// Line 192: Change default
public ScreenView CloseButton(string text = "\u2190 Back")

// Line 203: Change default
public ScreenView MenuButton(string text = "\u2630 Menu")
```

**Note:** For per-screen localization, the developer passes `ctx.GetLocalizedString("back")` as the `text` argument. `BotMessages` defaults apply only in framework-generated buttons (e.g., auto-added back button by ScreenManager). ScreenManager already has DI access and should use `BotMessages` for auto-generated buttons.

- [ ] **Step 2: Change PayloadExpiredException**

In `src/TelegramBotFlow.Core.Abstractions/Exceptions/PayloadExpiredException.cs`, line 9:

```csharp
// Change from Russian to English default:
public PayloadExpiredException()
    : base("Button data expired. Please refresh the menu.") { }
```

The message text is used for logging. The user-facing message comes from the handler catching the exception — and that should use `BotMessages.PayloadExpired`.

- [ ] **Step 3: Register BotMessages in DI**

In `src/TelegramBotFlow.Core/Extensions/ServiceCollectionExtensions.cs`, inside `AddTelegramBotFlow()`, add:

```csharp
services.Configure<BotMessages>(configuration.GetSection("Bot:Messages"));
```

This allows overriding from `appsettings.json` under `Bot:Messages:BackButton` etc., with POCO defaults as fallback.

- [ ] **Step 4: Build and test**

Run: `dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx`
Expected: All pass

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: replace hardcoded Russian strings with English defaults, wire BotMessages to DI"
```

---

### Task 5: Wire configurable parameters to consumers

**Files:**
- Modify: `src/TelegramBotFlow.Core.Abstractions/Sessions/NavigationState.cs` (line 90)
- Modify: `src/TelegramBotFlow.Core.Abstractions/Sessions/UserSession.cs` (line 13)
- Modify: `src/TelegramBotFlow.Core/Sessions/InMemorySessionLockProvider.cs` (line 15)
- Modify: `src/TelegramBotFlow.Core/Hosting/UpdateProcessingWorker.cs` (line 37)
- Modify: `src/TelegramBotFlow.Core/Extensions/ServiceCollectionExtensions.cs` (channel capacity)

- [ ] **Step 1: Make NavigationState payload cache size configurable**

`NavigationState` is in Abstractions (no DI). The cache size must be passed at construction time. Change `MAX_PAYLOADS` from const to a constructor parameter:

```csharp
// In NavigationState.cs, replace line 90:
// private const int MAX_PAYLOADS = 500;
// With:
private readonly int _maxPayloads;

// Add constructor parameter or property:
internal int MaxPayloads { get; set; } = 500;
```

Then in `StorePayloadJson`, use `MaxPayloads` instead of `MAX_PAYLOADS`.

**The value is set by SessionMiddleware** when it creates/loads the session — it reads `BotConfiguration.PayloadCacheSize` and sets `session.Navigation.MaxPayloads`.

- [ ] **Step 2: Make UserSession max navigation depth configurable**

Same pattern — `UserSession.MAX_NAVIGATION_DEPTH` becomes a settable property:

```csharp
// In UserSession.cs:
internal int MaxNavigationDepth { get; set; } = 20;
```

Set by SessionMiddleware after loading session.

- [ ] **Step 3: Wire InMemorySessionLockProvider to config**

In constructor, accept `IOptions<BotConfiguration>`:

```csharp
public InMemorySessionLockProvider(IOptions<BotConfiguration> config)
{
    _lockTimeout = TimeSpan.FromSeconds(config.Value.SessionLockTimeoutSeconds);
    // ...
}
```

- [ ] **Step 4: Wire UpdateProcessingWorker to config**

Replace hardcoded `100` with `config.MaxConcurrentUpdates`:

```csharp
// In ExecuteAsync, line ~37:
var options = new ParallelOptions
{
    MaxDegreeOfParallelism = _config.MaxConcurrentUpdates,
    CancellationToken = stoppingToken
};
```

- [ ] **Step 5: Wire channel capacity to config**

In `ServiceCollectionExtensions.AddTelegramBotFlow()`, replace hardcoded `1000`:

```csharp
var channel = Channel.CreateBounded<Update>(new BoundedChannelOptions(config.UpdateChannelCapacity)
{
    SingleWriter = true,
    FullMode = BoundedChannelFullMode.Wait
});
```

- [ ] **Step 6: Build and test**

Run: `dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx`
Expected: All pass

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: wire all configurable parameters from BotConfiguration to consumers"
```

---

### Task 6: Split BotApplication — move middleware to Builder

This is the breaking change. Do it in one focused task.

**Files:**
- Modify: `src/TelegramBotFlow.Core/Hosting/BotApplicationBuilder.cs`
- Modify: `src/TelegramBotFlow.Core/Hosting/BotApplication.cs`
- Modify: `src/TelegramBotFlow.Core/Hosting/BotRuntime.cs`
- Modify: `src/TelegramBotFlow.App/Program.cs`
- Modify: `tests/TelegramBotFlow.IntegrationTests/Helpers/BotWebApplicationFactory.cs`

- [ ] **Step 1: Move middleware registration to BotApplicationBuilder**

Add these methods to `BotApplicationBuilder.cs`:

```csharp
// Middleware state (add as private fields):
private readonly List<Func<UpdateDelegate, UpdateDelegate>> _middlewares = [];
private readonly List<string> _registeredMiddleware = [];

// Public middleware API (move from BotApplication):
public BotApplicationBuilder Use(Func<UpdateDelegate, UpdateDelegate> middleware) { ... }
public BotApplicationBuilder Use<TMiddleware>() where TMiddleware : IUpdateMiddleware { ... }
public BotApplicationBuilder UseErrorHandling() { ... }
public BotApplicationBuilder UseLogging() { ... }
public BotApplicationBuilder UsePrivateChatOnly() { ... }
public BotApplicationBuilder UseSession() { ... }
public BotApplicationBuilder UseWizards() { ... }
public BotApplicationBuilder UseAccessPolicy() { ... }
public BotApplicationBuilder UsePendingInput() { ... }
```

Each method returns `BotApplicationBuilder` (not `BotApplication`) for fluent chaining.

Move the implementation body from each method in `BotApplication` to the corresponding method in `BotApplicationBuilder`. The middleware factories and registered-middleware tracking list move to the builder.

- [ ] **Step 2: Update BotApplication — remove middleware methods**

Remove from `BotApplication.cs`:
- All `Use*()` methods (lines 71-151)
- `_middlewares` field
- `_registeredMiddleware` field

`BotApplication` keeps:
- All `Map*()` methods (routes)
- `UseNavigation<T>()` (route registration, not middleware)
- `SetMenu()`
- `RunAsync()`
- `MapBotEndpoints()`

The `Build()` method now receives middleware list from builder and passes it to `BotApplication` constructor.

```csharp
// BotApplication constructor becomes:
internal BotApplication(
    WebApplication app,
    UpdateRouter router,
    IReadOnlyList<Func<UpdateDelegate, UpdateDelegate>> middlewares,
    IList<string> registeredMiddleware)
```

- [ ] **Step 3: Update BotApplication.RunAsync()**

`RunAsync()` uses the middleware list received from builder:

```csharp
public async Task RunAsync()
{
    MiddlewareOrderValidator.Validate(_registeredMiddleware);
    var terminal = _router.BuildTerminal();
    var pipeline = UpdatePipeline.Build(_middlewares, terminal);
    // ... rest of runtime setup
}
```

- [ ] **Step 4: Update Program.cs for new API**

```csharp
// src/TelegramBotFlow.App/Program.cs
var builder = BotApplication.CreateBuilder(args);

// Serilog
builder.WebAppBuilder.Host.UseSerilog(...);

// Services
builder.Services.AddBotCoreData(builder.Configuration);
builder.Services.AddBotEndpoints(typeof(Program).Assembly);
builder.Services.AddScreens(typeof(Program).Assembly);
builder.Services.AddWizards(typeof(Program).Assembly);

// Middleware (NOW ON BUILDER)
builder.UseErrorHandling();
builder.UseLogging();
builder.UsePrivateChatOnly();
builder.UseSession();
builder.UseAccessPolicy();
builder.UseWizards();
builder.Use<UserTrackingMiddleware<BotUser>>();
builder.UsePendingInput();

// Build
var app = builder.Build();

// Routes (ON APP)
app.SetMenu(menu => menu.Command("start", "Main menu"));
app.UseNavigation<MainMenuScreen>();
app.MapBotEndpoints();

await app.RunAsync();
```

- [ ] **Step 5: Update BotWebApplicationFactory**

The test factory needs to work with the new builder/app split. Since it extends `WebApplicationFactory<Program>`, and `Program.cs` is the entry point, the factory should work if `Program.cs` is updated correctly. Verify that `ConfigureWebHost` overrides still apply.

- [ ] **Step 6: Build the solution**

Run: `dotnet build TelegramBotFlow.slnx`
Expected: Compilation succeeds. Fix any remaining references to old `app.Use*()` API in tests.

- [ ] **Step 7: Run all tests**

Run: `dotnet test TelegramBotFlow.slnx`
Expected: All tests pass

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor!: split BotApplication — middleware moves to BotApplicationBuilder

BREAKING CHANGE: Middleware registration (Use, UseErrorHandling, UseSession, etc.)
moves from BotApplication to BotApplicationBuilder. Route registration (MapCommand,
MapAction, UseNavigation) stays on BotApplication."
```

---

### Task 7: Update structured error logging

**Files:**
- Modify: `src/TelegramBotFlow.Core/Pipeline/Middlewares/ErrorHandlingMiddleware.cs`

- [ ] **Step 1: Enhance error logging with context**

In `ErrorHandlingMiddleware.InvokeAsync()`, update the `LogError` call to include structured context:

```csharp
_logger.LogError(ex,
    "Unhandled exception processing {UpdateType} from user {UserId} on screen {Screen}, handler {Handler}",
    context.UpdateType,
    context.UserId,
    context.Session?.Navigation.CurrentScreen,
    context.HandlerName);
```

- [ ] **Step 2: Build and test**

Run: `dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx`
Expected: All pass

- [ ] **Step 3: Commit**

```bash
git add src/TelegramBotFlow.Core/Pipeline/Middlewares/ErrorHandlingMiddleware.cs
git commit -m "feat: add structured error logging with handler name, screen, and user context"
```

---

### Task 8: Final verification

- [ ] **Step 1: Full build**

Run: `dotnet build TelegramBotFlow.slnx`
Expected: 0 warnings, 0 errors (TreatWarningsAsErrors=true)

- [ ] **Step 2: Full test suite**

Run: `dotnet test TelegramBotFlow.slnx`
Expected: All tests pass

- [ ] **Step 3: Verify example app compiles and starts**

Run: `dotnet build src/TelegramBotFlow.App`
Expected: Compiles without errors

- [ ] **Step 4: Commit tag**

```bash
git tag v2.0.0-alpha.1
```

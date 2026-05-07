# TelegramBotFlow Framework Overhaul — Design Spec

**Date:** 2026-04-14
**Status:** Approved
**Approach:** Phase-Based (4 phases: Cleanup → Bugfix → Architecture → Documentation)

## Context

TelegramBotFlow is a .NET Telegram bot framework with middleware pipeline, screen navigation, wizard FSM, and session management. A deep audit revealed critical bugs, unnecessary dependencies, and missing test coverage. This spec defines the full plan to bring the framework to production-ready, open-source quality.

### Key Decisions Made

- **Target:** Internal use first, public open-source release later. Build with open-source standards from day one.
- **Broadcasts module:** Remove entirely. Each bot implements its own broadcast logic. Core provides only low-level helpers (rate-limited sending via `IUpdateResponder`).
- **Core.Data:** Extract user storage into `IBotUserStore<TUser>` abstraction (like ASP.NET Identity). Provide `TelegramBotFlow.Data.Postgres` as optional EF Core implementation.
- **Wizard system:** Fix current bugs now, plan extensibility (MediaStep, LocationStep, etc.) for future iterations.
- **Screen navigation:** Keep single nav-message as default. Multi-message and toasts deferred to separate design.
- **SachkovTech.\* dependencies:** Remove completely (only used in Broadcasts, which is being deleted).

---

## Phase 1: Cleanup

**Goal:** Remove all unnecessary code and dependencies. Project compiles cleanly with zero external private packages.

### 1.1 Remove Broadcasts Module

- Delete `src/TelegramBotFlow.Broadcasts/` entirely
- Remove project reference from solution file
- Remove any Broadcasts references from App example
- Remove Quartz.NET dependencies (only used by Broadcasts)

### 1.2 Remove SachkovTech.\* Dependencies

- After Broadcasts removal, verify no .csproj references `SachkovTech.*`
- Remove from `nuget.config` if present as private feed
- Search all files for `using SachkovTech` — must be zero results

### 1.3 Namespace Consistency

- Unify all Core.Abstractions files to `TelegramBotFlow.Core.*` namespace (29 files already use this; fix the 3 that use `TelegramBotFlow.Core.Abstractions.*`)
- Specifically fix:
  - `Routing/BotResults.cs` — `TelegramBotFlow.Core.Abstractions.Routing` → `TelegramBotFlow.Core.Routing`
  - `Routing/IEndpointResult.cs` — same
  - `Screens/INavigationService.cs` — `TelegramBotFlow.Core.Abstractions.Screens` → `TelegramBotFlow.Core.Screens`
- Fix `BotExecutionContext.cs` import: `TelegramBotFlow.Core.Wizards` → verify consistent with abstractions namespace

### 1.4 Remove Dead Code

- `BotSettings` + `RoadmapMessageConfig` in Core.Data — bot-specific, not framework code. Delete.
- `BotSettingsConfiguration.cs` — delete
- All commented-out code blocks — delete
- Unused using directives — clean up
- Remove `BotSettings` DbSet from `BotDbContext`

### 1.5 Clean App Example

- Remove Broadcasts references
- Verify App compiles and demonstrates: screens, wizards, commands, callbacks, input handling
- Remove any bot-specific business logic that leaked into the example

### Exit Criteria

- [ ] Solution compiles with `dotnet build`
- [ ] All existing tests pass with `dotnet test`
- [ ] Zero references to `SachkovTech.*`
- [ ] Zero references to `TelegramBotFlow.Broadcasts`
- [ ] No commented-out code blocks
- [ ] Namespace grep shows consistent pattern

---

## Phase 2: Bugfix + Hardening

**Goal:** Fix all bugs found in audit. Achieve >85% test coverage. Framework is safe for production.

### 2.1 Critical Bugs

#### 2.1.1 Session Lock Scope (SessionMiddleware)

**Bug:** Lock is released (via `using`) before `await next(context)` completes. All middleware and handlers run without session lock protection.

**Fix:** Restructure to hold lock for entire pipeline execution:
```csharp
public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
{
    using var sessionLock = await _lockProvider.AcquireLockAsync(context.UserId, ct);
    var session = await _sessionStore.GetOrCreateAsync(context.UserId, ct);
    context.Session = session;

    try
    {
        await next(context);
    }
    finally
    {
        await _sessionStore.SaveAsync(session, ct);
    }
    // Lock released here, after save
}
```

#### 2.1.2 Wizard State Deserialization (BotWizard\<TState\>)

**Bug:** `JsonSerializer.Deserialize<TState>()` with `!` null-forgiving operator. Crashes on malformed JSON.

**Fix:**
```csharp
TState state;
try
{
    state = string.IsNullOrWhiteSpace(storageState.PayloadJson)
        ? new TState()
        : JsonSerializer.Deserialize<TState>(storageState.PayloadJson) ?? new TState();
}
catch (JsonException ex)
{
    _logger.LogWarning(ex, "Failed to deserialize wizard state for {WizardId}, terminating wizard", wizardId);
    await _wizardStore.DeleteAsync(context.UserId);
    return new WizardTransition(true, BotResults.Back());
}
```

#### 2.1.3 OnEnter Partial State Corruption

**Bug:** If `OnEnter` throws after mutating state, the partially-modified state is serialized and persisted.

**Fix:** Serialize state ONLY after successful OnEnter. Clone state before OnEnter, restore on failure:
```csharp
string stateBeforeEnter = JsonSerializer.Serialize(state);
try
{
    if (nextStep.OnEnter is not null)
        await nextStep.OnEnter(context, state);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "OnEnter failed for step {StepId}, reverting state", goTo.StepId);
    state = JsonSerializer.Deserialize<TState>(stateBeforeEnter)!;
    // Stay on current step
    return new WizardTransition(false, BotResults.Stay());
}
storageState.PayloadJson = JsonSerializer.Serialize(state);
```

#### 2.1.4 NavigationStack Mutation Safety

**Bug:** `public List<string> NavigationStack` exposes mutable collection.

**Fix:**
```csharp
private readonly List<string> _navigationStack = [];

[JsonInclude]
public IReadOnlyList<string> NavigationStack => _navigationStack;

internal void PushScreen(string screenId) { _navigationStack.Add(...); }
internal string? PopScreen() { ... _navigationStack.RemoveAt(...); }
```

Use `[JsonInclude]` + `[JsonPropertyName]` for serialization compatibility with Redis store.

### 2.2 Medium Bugs

#### 2.2.1 NavigateBackAsync Null Session

**File:** `NavigationService.cs`
**Fix:** Add null-check before `PopScreen()`. Log warning if session is null.

#### 2.2.2 Payload Expiration Logging

**File:** `PayloadDelegateFactory.cs`
**Fix:** Log warning on `PayloadExpiredException`. Answer callback with alert "Кнопка устарела. Обновите экран."

#### 2.2.3 Input Handler Not Found

**File:** `PendingInputMiddleware.cs`
**Fix:** Log `LogWarning("Input handler '{ActionId}' not found, falling through to router", actionId)`.

#### 2.2.4 UserTrackingMiddleware Memory Leak

**File:** `UserTrackingMiddleware.cs`
**Bug:** Static `ConcurrentDictionary<long, byte> _knownUsers` grows unbounded.
**Fix:** Replace with `MemoryCache` with 1-hour sliding expiration. Or use `HashSet` with periodic cleanup.

#### 2.2.5 Striped Lock Distribution

**File:** `InMemorySessionLockProvider.cs`
**Fix:** Replace `Math.Abs(userId.GetHashCode()) % STRIPE_COUNT` with `(int)((ulong)userId % (ulong)STRIPE_COUNT)` for better distribution.

#### 2.2.6 Lock Timeout Configurability

**File:** `InMemorySessionLockProvider.cs`
**Fix:** Accept `TimeSpan lockTimeout` via constructor/options instead of hardcoded 10 seconds.

#### 2.2.7 NavMessageId Delete Error Handling

**File:** `UpdateResponder.cs`
**Fix:** Catch 429 (rate limit) in addition to 400/403. For 400, check message contains "message to delete not found" for specificity.

### 2.3 New Test Coverage

#### Middleware Unit Tests

| Test Class | Tests |
|---|---|
| `ErrorHandlingMiddlewareTests` | Catches exception + sends error message to user; Re-throws after notification; Handles null session gracefully |
| `SessionMiddlewareTests` | Loads session before next(); Saves session after next(); Lock held during entire pipeline; Lock timeout throws TimeoutException; Session created if not exists |
| `PendingInputMiddlewareTests` | Command bypasses pending input; Text routed to registered handler; Unregistered handler falls through with warning; No pending → passes to next |
| `WizardMiddlewareTests` | Active wizard intercepts all updates; /cancel terminates wizard; nav:back in wizard goes to previous step; No active wizard → passes to next |
| `PrivateChatOnlyMiddlewareTests` | Blocks group chat; Blocks channel; Allows private chat |

#### Error Path Tests

| Scenario | Expected Behavior |
|---|---|
| Session store throws on GetOrCreate | Middleware logs error, does not crash; user gets error message |
| Session store throws on Save | Logs error; lock still released; no data loss for current request |
| Wizard deserialization fails | Wizard terminated gracefully; user navigated back |
| Screen not found in registry | Clear error message logged; user gets "Screen not found" |
| Payload expired | Callback answered with alert; user prompted to refresh |
| Handler compilation fails | Startup exception with clear message naming the handler |

#### Concurrency Tests

| Scenario | Expected Behavior |
|---|---|
| Two updates from same user | Second waits for lock; both process correctly |
| Session state preserved across concurrent access | No data loss; last-write-wins within lock |
| Lock timeout | TimeoutException with user ID in message |

#### Screen Navigation Edge Cases

| Scenario | Expected Behavior |
|---|---|
| Media type Photo→Video | Delete old message + send new |
| Media type None→Photo | Delete old message + send new |
| Navigation beyond max depth (20) | Oldest entry dropped from stack |
| Back on empty stack | No-op; stays on current screen |
| NavigateToRoot with empty stack | Sets screen as root; stack cleared |

#### Wizard Edge Cases

| Scenario | Expected Behavior |
|---|---|
| GoBack on first step | Wizard terminates; NavigateBack result |
| Expired wizard (TTL passed) | Cleanup from store; NavigateBack |
| State with null fields | Deserializes correctly with defaults |
| Invalid step ID in GoTo | Wizard terminates with error log |

### Exit Criteria

- [ ] All critical bugs fixed
- [ ] All medium bugs fixed
- [ ] >85% line coverage on Core
- [ ] >70% line coverage on Core.Abstractions
- [ ] All new tests pass
- [ ] No warnings in build output
- [ ] Manual smoke test: start bot, navigate screens, complete wizard, test edge cases

---

## Phase 3: Architecture + API

**Goal:** Clean, extensible API. User storage abstraction. Production features.

### 3.1 IBotUserStore\<TUser\> Abstraction

**In Core.Abstractions:**

```csharp
public interface IBotUser
{
    long TelegramId { get; }
    bool IsBlocked { get; set; }
    DateTime JoinedAt { get; }
}

public interface IBotUserStore<TUser> where TUser : class, IBotUser
{
    Task<TUser?> FindByTelegramIdAsync(long telegramId, CancellationToken ct = default);
    Task CreateAsync(TUser user, CancellationToken ct = default);
    Task UpdateAsync(TUser user, CancellationToken ct = default);
    Task MarkBlockedAsync(long telegramId, CancellationToken ct = default);
}
```

**In Core:**
- `UserTrackingMiddleware<TUser>` — generic, uses `IBotUserStore<TUser>`
- Sets `context.BotUser` (new property on `UpdateContext` or accessible via `context.Session.Data`)
- Creates user on first interaction, updates `LastActivity` on every update

**Registration:**
```csharp
// In Core
services.AddBotUserTracking<MyBotUser>();

// In Data.Postgres
services.AddBotUserStore<MyBotUser>()
        .AddEntityFrameworkStore<MyBotDbContext>();
```

### 3.2 TelegramBotFlow.Data.Postgres Package

New NuGet package:

```
TelegramBotFlow.Data.Postgres/
├── EfBotUserStore.cs           // IBotUserStore<TUser> via EF Core
├── BotDbContext.cs             // Generic DbContext<TUser>
├── BotUserConfiguration.cs    // Base entity config
├── DependencyInjection.cs     // AddEntityFrameworkStore<TContext>() extension
└── TelegramBotFlow.Data.Postgres.csproj
```

**Dependencies:** EF Core, Npgsql.EntityFrameworkCore.PostgreSQL
**No migrations included** — consumer creates their own migrations in their project.

### 3.3 Webhook Secret Token

```csharp
public class BotOptions
{
    public string Token { get; set; } = "";
    public string? WebhookUrl { get; set; }
    public string? WebhookSecretToken { get; set; }  // NEW
}
```

- If `WebhookSecretToken` is set, webhook endpoint validates `X-Telegram-Bot-Api-Secret-Token` header
- Token passed to `bot.SetWebhook(url, secretToken: options.WebhookSecretToken)` on startup
- Validation as endpoint filter (Minimal APIs) or middleware

### 3.4 Payload TTL + Configurable Cache Size

```csharp
public class BotOptions
{
    // ...existing...
    public int PayloadMaxCount { get; set; } = 500;
    public TimeSpan? PayloadTtl { get; set; } = null;  // null = no TTL
}
```

- `NavigationState` reads max count from options (passed via constructor or session factory)
- On access: check TTL if set; throw `PayloadExpiredException` if expired
- On eviction from LRU: log at Debug level

### 3.5 Explicit Middleware Ordering with Validation

Replace raw `Use<T>()` with semantic helpers:

```csharp
public sealed class BotApplication
{
    // Semantic middleware registration
    public BotApplication UseErrorHandling();
    public BotApplication UseLogging();
    public BotApplication UsePrivateChatOnly();
    public BotApplication UseSessions();          // marks "sessions enabled"
    public BotApplication UseAccessPolicy();
    public BotApplication UseWizards();            // requires sessions
    public BotApplication UsePendingInput();       // requires sessions
    public BotApplication UseUserTracking<TUser>() where TUser : class, IBotUser;

    // Raw middleware for custom use
    public BotApplication Use<TMiddleware>() where TMiddleware : IUpdateMiddleware;

    // Validation at startup
    public async Task RunAsync()
    {
        ValidateMiddlewareOrder();  // throws if wizards before sessions, etc.
        // ...
    }
}
```

Validation rules:
- `UseWizards()` requires `UseSessions()` registered before it
- `UsePendingInput()` requires `UseSessions()` registered before it
- `UseUserTracking()` requires `UseSessions()` registered before it
- `UseErrorHandling()` should be first (warn if not)

### 3.6 Action ID Safety

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ActionIdAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}

// Usage
[ActionId("submit-feedback")]
public struct SubmitFeedbackAction : IBotAction;
```

- If `[ActionId]` present → use it
- If not → use `typeof(T).Name` (backward compatible)
- At registration: validate uniqueness, throw `InvalidOperationException` on duplicate

### 3.7 Screen ID Explicit Override

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class ScreenIdAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}

// Usage
[ScreenId("user-profile")]
public class ProfileScreen : IScreen { ... }
```

- If `[ScreenId]` present → use it
- If not → use `ScreenIdConvention` (ClassName → snake_case)
- At registration: validate uniqueness

### 3.8 App Example Update

Restructure example to demonstrate all patterns:

```
TelegramBotFlow.App/
├── Program.cs                     // Clean setup with all features
├── BotUser.cs                     // Custom user extending IBotUser
├── AppDbContext.cs                // EF Core context
├── Screens/
│   ├── MainMenuScreen.cs
│   ├── ProfileScreen.cs
│   └── SettingsScreen.cs
├── Wizards/
│   ├── ProfileSetupWizard.cs
│   └── FeedbackWizard.cs
├── Commands/
│   ├── StartEndpoint.cs
│   └── HelpEndpoint.cs
├── Middleware/
│   └── LanguageDetectionMiddleware.cs  // Custom middleware example
└── appsettings.json                    // Configuration example
```

### Exit Criteria

- [ ] `IBotUserStore<TUser>` interface in Core.Abstractions
- [ ] `TelegramBotFlow.Data.Postgres` package compiles independently
- [ ] Webhook secret token works in webhook mode
- [ ] Payload TTL configurable
- [ ] Middleware ordering validated at startup
- [ ] Action/Screen ID attributes work
- [ ] App example demonstrates all patterns
- [ ] All existing + new tests pass

---

## Phase 4: Documentation + Polish

**Goal:** Framework fully documented for humans and AI agents. Ready for open-source release.

### 4.1 CLAUDE.md

Location: `/CLAUDE.md` (root of telegram-bot-flow repo)

Contents:
- Project structure (packages, dependencies between them)
- Build & test commands (`dotnet build`, `dotnet test`, `dotnet pack`)
- Architecture overview (pipeline → middleware → router → handler → result)
- Key patterns:
  - Adding a command: create class implementing endpoint, `MapCommand("name")`
  - Adding a screen: implement `IScreen`, return `ScreenView`
  - Adding a wizard: extend `BotWizard<TState>`, configure steps
  - Adding middleware: implement `IUpdateMiddleware`, register with `Use<T>()`
  - Adding input handler: `MapInput<TAction>(handler)`
- Known gotchas:
  - Middleware ordering (sessions before wizards)
  - Payload encoding (inline <64 bytes, stored ≥64 bytes)
  - Session lock scope (held for entire pipeline)
  - Screen ID convention (ClassName → snake_case, override with `[ScreenId]`)
  - Callback data 64-byte Telegram limit
- Post-change checklist:
  - `dotnet build` passes
  - `dotnet test` passes
  - XML-docs on new public API
  - Update App example if API changed

### 4.2 AGENTS.md

Location: `/AGENTS.md` (root)

Contents:
- Framework domain model (Update → Pipeline → Middleware → Router → Handler → Result → Screen)
- Component responsibilities table
- Package dependency graph
- Extension points (custom middleware, screens, wizards, user store)
- Session lifecycle diagram
- Wizard state machine diagram

### 4.3 README.md (English)

Location: `/README.md` (root)

Sections:
- Badge row (build status, NuGet version, license)
- One-paragraph description
- Quick Start (minimal bot in ~15 lines)
- Features list with links to detailed docs
- Installation (NuGet packages)
- Concepts:
  - Pipeline & Middleware
  - Screens & Navigation
  - Wizards (FSM)
  - Sessions & State
  - User Storage
- Configuration reference table
- Examples (link to App project)
- Contributing (placeholder)
- License (MIT)

### 4.4 XML Documentation

All public types in Core.Abstractions and Core:
- Interfaces: summary + param + returns on every method
- Records/classes: summary + remarks where non-obvious
- Enums: summary on each value
- Static helpers: summary + example usage

Language: English (for NuGet/IDE tooltip compatibility).

### 4.5 CHANGELOG.md

```markdown
# Changelog

## [1.0.0] - 2026-XX-XX

### Removed
- Broadcasts module (application-specific, not framework concern)
- SachkovTech.* dependencies
- BotSettings/RoadmapMessageConfig (bot-specific)

### Fixed
- Session lock scope — lock now held for entire middleware pipeline
- Wizard state deserialization crash on malformed JSON
- OnEnter partial state corruption on exception
- NavigationStack exposed as mutable List
- UserTrackingMiddleware unbounded memory growth
- Striped lock poor distribution for user IDs
- NavigateBackAsync null session crash

### Added
- IBotUserStore<TUser> abstraction (Identity-style)
- TelegramBotFlow.Data.Postgres package
- Webhook secret token validation
- Configurable payload TTL and cache size
- Explicit middleware ordering with validation
- [ActionId] and [ScreenId] attributes
- Comprehensive test coverage (>85%)
- CLAUDE.md, AGENTS.md documentation

### Changed
- NavigationStack is now IReadOnlyList<string>
- Middleware registration via semantic helpers (UseSessions, UseWizards, etc.)
- Lock timeout is now configurable
```

### 4.6 CI Setup

**GitLab CI** (matching existing infrastructure):
```yaml
stages:
  - build
  - test
  - pack

build:
  stage: build
  script: dotnet build -c Release

test:
  stage: test
  script: dotnet test -c Release --logger "trx"
  services:
    - redis:7-alpine  # for Redis integration tests

pack:
  stage: pack
  script: dotnet pack -c Release -o ./artifacts
  only:
    - tags
```

### Exit Criteria

- [ ] CLAUDE.md written and comprehensive
- [ ] AGENTS.md written
- [ ] README.md in English
- [ ] XML-docs on all public API
- [ ] CHANGELOG.md with v1.0.0 entry
- [ ] CI pipeline runs build + test
- [ ] `dotnet pack` produces valid NuGet packages
- [ ] Final manual review of all documentation

---

## Future Work (Not In Scope)

These items are documented for future design specs:

- **Wizard extensions:** MediaStep, LocationStep, ContactStep, ButtonStep improvements
- **Screen navigation:** Multi-message support, toast/notification messages, media albums
- **Inline mode:** Inline query handling and results
- **Group/channel support:** Per-chat-type routing, admin commands in groups
- **Distributed sessions:** Redis-based locking for multi-instance deployments
- **Metrics/observability:** OpenTelemetry integration, update processing metrics
- **Hot-reload:** Screen/command hot-reload in development mode
- **Testing utilities:** `BotTestClient` for end-to-end testing without Telegram API

---

## Package Structure (Final)

```
TelegramBotFlow/
├── src/
│   ├── TelegramBotFlow.Core.Abstractions/   # Interfaces, models, no deps
│   ├── TelegramBotFlow.Core/                # Framework implementation
│   ├── TelegramBotFlow.Core.Redis/          # Redis session store (optional)
│   ├── TelegramBotFlow.Data.Postgres/       # EF Core user store (optional)
│   └── TelegramBotFlow.App/                 # Example application
├── tests/
│   ├── TelegramBotFlow.Core.Tests/          # Unit tests
│   └── TelegramBotFlow.IntegrationTests/    # Integration tests
├── docs/
│   ├── ARCHITECTURE.md
│   ├── USAGE.md
│   ├── API.md
│   ├── CODE_STYLE.md
│   └── specs/
├── CLAUDE.md
├── AGENTS.md
├── README.md
├── CHANGELOG.md
└── LICENSE
```

## Dependency Graph

```
Core.Abstractions (no external deps except Telegram.Bot + DI.Abstractions)
    ↑
    Core (implements abstractions)
    ↑           ↑
Core.Redis    Data.Postgres
(optional)    (optional)
    ↑           ↑
    App (example, references all)
```

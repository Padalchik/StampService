# Phase 2: Bugfix + Hardening — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix all critical and medium bugs found in audit, add comprehensive test coverage (>85%), make framework production-safe.

**Architecture:** Fix bugs in-place following existing patterns. Tests use xUnit + NSubstitute + FluentAssertions, matching existing test infrastructure. No new abstractions — just correctness and coverage.

**Tech Stack:** .NET 10, Telegram.Bot 22.9.0, xUnit, NSubstitute, FluentAssertions

---

### Task 1: Fix SessionMiddleware lock scope

The lock is released before `await next(context)` completes. All middleware and handlers run without session lock protection. Fix: hold lock for entire pipeline, save session in finally block.

**Files:**
- Modify: `src/TelegramBotFlow.Core/Pipeline/Middlewares/SessionMiddleware.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Pipeline/SessionMiddlewareTests.cs`

- [ ] **Step 1: Write failing test — lock held during pipeline execution**

Create `tests/TelegramBotFlow.Core.Tests/Pipeline/SessionMiddlewareTests.cs`:

```csharp
using NSubstitute;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Pipeline;

public class SessionMiddlewareTests
{
    private readonly ISessionStore _sessionStore = Substitute.For<ISessionStore>();
    private readonly ISessionLockProvider _lockProvider = Substitute.For<ISessionLockProvider>();
    private readonly IDisposable _lock = Substitute.For<IDisposable>();

    public SessionMiddlewareTests()
    {
        _lockProvider.AcquireLockAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_lock);
        _sessionStore.GetOrCreateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new UserSession());
    }

    [Fact]
    public async Task Session_is_saved_after_pipeline_completes()
    {
        var middleware = new SessionMiddleware(_sessionStore, _lockProvider);
        var context = TestHelpers.CreateContext(userId: 123);

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        await _sessionStore.Received(1).SaveAsync(Arg.Any<UserSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Session_is_saved_even_when_pipeline_throws()
    {
        var middleware = new SessionMiddleware(_sessionStore, _lockProvider);
        var context = TestHelpers.CreateContext(userId: 123);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(context, _ => throw new InvalidOperationException("boom")));

        await _sessionStore.Received(1).SaveAsync(Arg.Any<UserSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Lock_is_released_after_save()
    {
        var middleware = new SessionMiddleware(_sessionStore, _lockProvider);
        var context = TestHelpers.CreateContext(userId: 123);

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Received.InOrder(() =>
        {
            _sessionStore.SaveAsync(Arg.Any<UserSession>(), Arg.Any<CancellationToken>());
            _lock.Dispose();
        });
    }

    [Fact]
    public async Task Session_is_set_on_context_before_next()
    {
        var middleware = new SessionMiddleware(_sessionStore, _lockProvider);
        var context = TestHelpers.CreateContext(userId: 123);
        UserSession? capturedSession = null;

        await middleware.InvokeAsync(context, ctx =>
        {
            capturedSession = ctx.Session;
            return Task.CompletedTask;
        });

        Assert.NotNull(capturedSession);
    }

    [Fact]
    public async Task Skips_session_for_zero_userId()
    {
        var middleware = new SessionMiddleware(_sessionStore, _lockProvider);
        var context = TestHelpers.CreateContext(userId: 0);
        bool nextCalled = false;

        await middleware.InvokeAsync(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
        await _lockProvider.DidNotReceive().AcquireLockAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
cd "/Users/dev/code/miracle generation/telegram-bot-flow"
dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~SessionMiddlewareTests"
```

Expected: `Session_is_saved_even_when_pipeline_throws` FAILS (currently save is not in finally block).

- [ ] **Step 3: Fix SessionMiddleware**

Replace the `InvokeAsync` method in `src/TelegramBotFlow.Core/Pipeline/Middlewares/SessionMiddleware.cs`:

```csharp
    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        if (context.UserId == 0)
        {
            await next(context);
            return;
        }

        using IDisposable sessionLock = await _lockProvider.AcquireLockAsync(context.UserId, context.CancellationToken);

        UserSession session = await _sessionStore.GetOrCreateAsync(context.UserId, context.CancellationToken);
        context.Session = session;

        try
        {
            await next(context);
        }
        finally
        {
            await _sessionStore.SaveAsync(session, context.CancellationToken);
        }
    }
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~SessionMiddlewareTests"
```

Expected: All 5 tests PASS.

- [ ] **Step 5: Run full test suite**

```bash
dotnet test TelegramBotFlow.slnx
```

Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "fix: hold session lock for entire middleware pipeline

Previously lock was released before await next(context) completed.
Now session is saved in finally block, lock released after save."
```

---

### Task 2: Fix BotWizard deserialization and OnEnter state corruption

Two related bugs in BotWizard: (1) JsonSerializer.Deserialize crashes on malformed JSON, (2) OnEnter can partially corrupt state if it throws.

**Files:**
- Modify: `src/TelegramBotFlow.Core/Wizards/BotWizard.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Wizards/BotWizardDeserializationTests.cs`

- [ ] **Step 1: Write failing test — malformed JSON terminates wizard gracefully**

Create `tests/TelegramBotFlow.Core.Tests/Wizards/BotWizardDeserializationTests.cs`:

```csharp
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Tests.Wizards;

public class BotWizardDeserializationTests
{
    private sealed class TestState : class, new()
    {
        public string? Name { get; set; }
    }

    private sealed class TestWizard : BotWizard<TestState>
    {
        protected override void ConfigureSteps(WizardBuilder<TestState> builder)
        {
            builder.TextStep("step1", (_, _) => ValueTask.FromResult(new ScreenView("Enter name")),
                (_, state) => { state.Name = "test"; return Task.FromResult(StepResult.Finish()); });
        }

        public override Task<IEndpointResult> OnFinishedAsync(UpdateContext context, TestState state)
            => Task.FromResult(BotResults.Back());
    }

    [Fact]
    public async Task Malformed_json_terminates_wizard_gracefully()
    {
        var wizard = new TestWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            PayloadJson = "{invalid json!!!"
        };
        var context = TestHelpers.CreateContext();

        WizardTransition transition = await wizard.ProcessUpdateAsync(context, storageState);

        Assert.True(transition.IsFinished);
    }

    [Fact]
    public async Task Null_json_creates_default_state()
    {
        var wizard = new TestWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            PayloadJson = null!
        };
        var context = TestHelpers.CreateContext();

        WizardTransition transition = await wizard.ProcessUpdateAsync(context, storageState);

        // Should not throw — creates new TestState()
        Assert.True(transition.IsFinished);
    }
}
```

NOTE: The test file above uses simplified patterns. The actual test will need proper `using` statements matching the project's conventions. Use `TestHelpers.CreateContext()` which already exists in the test project. The `TestState` class needs `class` constraint to match `BotWizard<TState> where TState : class, new()`. Adapt the test to compile with the actual wizard API — the key assertion is that malformed JSON does NOT throw but terminates the wizard.

- [ ] **Step 2: Fix BotWizard.ProcessUpdateAsync — wrap deserialization in try-catch**

In `src/TelegramBotFlow.Core/Wizards/BotWizard.cs`, add `using Microsoft.Extensions.Logging;` and `using System.Text.Json;` (already present). Replace the state deserialization block in `ProcessUpdateAsync`:

Current code (line ~79):
```csharp
        TState state = string.IsNullOrWhiteSpace(storageState.PayloadJson)
            ? new TState()
            : JsonSerializer.Deserialize<TState>(storageState.PayloadJson)!;
```

Replace with:
```csharp
        TState state;
        try
        {
            state = string.IsNullOrWhiteSpace(storageState.PayloadJson)
                ? new TState()
                : JsonSerializer.Deserialize<TState>(storageState.PayloadJson) ?? new TState();
        }
        catch (JsonException)
        {
            return new WizardTransition(true, BotResults.Back());
        }
```

Apply the same fix in `GoBackAsync` (line ~138):

Current code:
```csharp
        TState state = string.IsNullOrWhiteSpace(storageState.PayloadJson)
            ? new TState()
            : JsonSerializer.Deserialize<TState>(storageState.PayloadJson)!;
```

Replace with:
```csharp
        TState state;
        try
        {
            state = string.IsNullOrWhiteSpace(storageState.PayloadJson)
                ? new TState()
                : JsonSerializer.Deserialize<TState>(storageState.PayloadJson) ?? new TState();
        }
        catch (JsonException)
        {
            return new WizardTransition(true, BotResults.Back());
        }
```

- [ ] **Step 3: Fix OnEnter partial state corruption**

In `ProcessUpdateAsync`, find the OnEnter block inside the `GoToResult` branch:

Current code:
```csharp
            if (nextStep.OnEnter is not null)
            {
                await nextStep.OnEnter(context, state);
                storageState.PayloadJson = JsonSerializer.Serialize(state);
            }
```

Replace with:
```csharp
            if (nextStep.OnEnter is not null)
            {
                string stateBeforeEnter = JsonSerializer.Serialize(state);
                try
                {
                    await nextStep.OnEnter(context, state);
                }
                catch (Exception)
                {
                    state = JsonSerializer.Deserialize<TState>(stateBeforeEnter) ?? new TState();
                    ScreenView stayView = await currentStep.Renderer(context, state);
                    return new WizardTransition(false, BotResults.ShowView(stayView));
                }
                storageState.PayloadJson = JsonSerializer.Serialize(state);
            }
```

Apply the same pattern in `InitializeAsync` for the initial step's OnEnter:

Current code:
```csharp
        if (initialStep.OnEnter is not null)
            await initialStep.OnEnter(context, state);
```

Replace with:
```csharp
        if (initialStep.OnEnter is not null)
        {
            try
            {
                await initialStep.OnEnter(context, state);
            }
            catch (Exception)
            {
                return new WizardTransition(true, BotResults.Back());
            }
        }
```

- [ ] **Step 4: Run tests**

```bash
dotnet test TelegramBotFlow.slnx
```

Expected: All tests pass (existing + new).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "fix: wizard state deserialization and OnEnter corruption

- Wrap JSON deserialization in try-catch; malformed JSON terminates wizard
- Snapshot state before OnEnter; revert on exception to prevent corruption"
```

---

### Task 3: Fix NavigationStack mutation safety

`public List<string> NavigationStack` exposes mutable collection. External code can bypass internal mutation guards. Fix: return `IReadOnlyList<string>`, use private backing field.

**Files:**
- Modify: `src/TelegramBotFlow.Core.Abstractions/Sessions/NavigationState.cs`
- Modify: `src/TelegramBotFlow.Core.Redis/RedisSessionStore.cs` (serialization compatibility)

- [ ] **Step 1: Change NavigationStack to IReadOnlyList with private backing field**

In `src/TelegramBotFlow.Core.Abstractions/Sessions/NavigationState.cs`, replace:

```csharp
    public List<string> NavigationStack { get; internal set; } = [];
```

with:

```csharp
    private readonly List<string> _navigationStack = [];

    /// <summary>
    /// Стек истории навигации (список screen ID в порядке посещения).
    /// Используй <c>BotResults.Back()</c> вместо прямого изменения стека.
    /// </summary>
    public IReadOnlyList<string> NavigationStack => _navigationStack;
```

Then update ALL internal references from `NavigationStack` to `_navigationStack` within the same file. Specifically:

- `PushScreen`: `NavigationStack.IndexOf(screenId)` → `_navigationStack.IndexOf(screenId)`
- `PushScreen`: `NavigationStack.RemoveRange(...)` → `_navigationStack.RemoveRange(...)`
- `PushScreen`: `NavigationStack.Add(CurrentScreen)` → `_navigationStack.Add(CurrentScreen)`
- `PushScreen`: `NavigationStack.Count` → `_navigationStack.Count`
- `PushScreen`: `NavigationStack.RemoveRange(0, ...)` → `_navigationStack.RemoveRange(0, ...)`
- `PopScreen`: `NavigationStack.Count` → `_navigationStack.Count`
- `PopScreen`: `NavigationStack[^1]` → `_navigationStack[^1]`
- `PopScreen`: `NavigationStack.RemoveAt(...)` → `_navigationStack.RemoveAt(...)`
- `Reset`: `NavigationStack.Clear()` → `_navigationStack.Clear()`
- `Clear`: `NavigationStack.Clear()` → `_navigationStack.Clear()`

- [ ] **Step 2: Add internal method for deserialization (Redis store needs to populate the list)**

Add this method to `NavigationState`:

```csharp
    internal void PopulateNavigationStack(IEnumerable<string> screenIds)
    {
        _navigationStack.Clear();
        _navigationStack.AddRange(screenIds);
    }
```

- [ ] **Step 3: Update RedisSessionStore deserialization**

In `src/TelegramBotFlow.Core.Redis/RedisSessionStore.cs`, find where `NavigationStack` is set during deserialization and use the new `PopulateNavigationStack` method instead of direct assignment. Search for `NavigationStack =` or similar patterns and replace with the populate method call.

- [ ] **Step 4: Update NavigationService.NavigateBackAsync**

In `src/TelegramBotFlow.Core/Screens/NavigationService.cs`, the `NavigateBackAsync` method accesses `NavigationStack[^1]` which works fine with `IReadOnlyList<string>` (indexer is supported). No change needed — but verify it compiles.

- [ ] **Step 5: Build and test**

```bash
dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx
```

Expected: All tests pass. If any test directly mutated `NavigationStack` via `.Add()` or `.Clear()`, it will fail at compile time — fix those tests to use the internal methods via reflection or by testing through the public API (PushScreen/PopScreen).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "fix: make NavigationStack read-only to prevent external mutation

NavigationStack now returns IReadOnlyList<string>. Internal mutations
use private _navigationStack field. PopulateNavigationStack added
for deserialization."
```

---

### Task 4: Fix all medium bugs

Seven small fixes batched together. Each is a 1-3 line change.

**Files:**
- Modify: `src/TelegramBotFlow.Core/Screens/NavigationService.cs` (2.2.1 null session)
- Modify: `src/TelegramBotFlow.Core/Pipeline/Middlewares/PendingInputMiddleware.cs` (2.2.3 logging)
- Modify: `src/TelegramBotFlow.Core.Data/Middleware/UserTrackingMiddleware.cs` (2.2.4 memory leak)
- Modify: `src/TelegramBotFlow.Core/Sessions/InMemorySessionLockProvider.cs` (2.2.5 distribution + 2.2.6 configurable timeout)
- Modify: `src/TelegramBotFlow.Core/Context/UpdateResponder.cs` (2.2.7 error handling)

- [ ] **Step 1: Fix NavigateBackAsync null session (2.2.1)**

In `src/TelegramBotFlow.Core/Screens/NavigationService.cs`, replace the `NavigateBackAsync` method:

```csharp
    public async Task NavigateBackAsync(UpdateContext context)
    {
        if (context.Session is null)
            return;

        string? previousScreen = context.Session.Navigation.NavigationStack is { Count: > 0 }
            ? context.Session.Navigation.NavigationStack[^1]
            : context.Session.Navigation.CurrentScreen;

        if (previousScreen is not null)
        {
            context.Session.Navigation.PopScreen();
            await _screenManager.RenderScreenAsync(context, previousScreen, pushToStack: false);
        }
    }
```

- [ ] **Step 2: Fix PendingInputMiddleware missing log (2.2.3)**

In `src/TelegramBotFlow.Core/Pipeline/Middlewares/PendingInputMiddleware.cs`, add `ILogger` dependency and log when handler not found.

Add field and constructor parameter:
```csharp
    private readonly InputHandlerRegistry _registry;
    private readonly ILogger<PendingInputMiddleware> _logger;

    public PendingInputMiddleware(InputHandlerRegistry registry, ILogger<PendingInputMiddleware> logger)
    {
        _registry = registry;
        _logger = logger;
    }
```

Add logging where handler is null:
```csharp
        UpdateDelegate? handler = _registry.Find(actionId);
        if (handler is null)
        {
            _logger.LogWarning("Input handler '{ActionId}' not found, falling through to router", actionId);

            if (context.Session is not null)
                context.Session.Navigation.PendingInputActionId = null;

            await next(context);
            return;
        }
```

- [ ] **Step 3: Fix UserTrackingMiddleware memory leak (2.2.4)**

In `src/TelegramBotFlow.Core.Data/Middleware/UserTrackingMiddleware.cs`, replace `ConcurrentDictionary` with `MemoryCache`:

Add using: `using Microsoft.Extensions.Caching.Memory;`

Replace the static field and check logic in `UserTrackingMiddleware<TUser>`:

```csharp
    private static readonly MemoryCache _knownUsers = new(new MemoryCacheOptions());
    private static readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);
```

Replace the check:
```csharp
        if (context.UserId != 0)
        {
            long userId = context.UserId;
            if (!_knownUsers.TryGetValue(userId, out _))
            {
                bool exists = await _db.Users.AnyAsync(u => u.TelegramId == userId);
                if (!exists)
                {
                    _db.Users.Add(new TUser { TelegramId = userId });
                    await _db.SaveChangesAsync();
                }

                _knownUsers.Set(userId, true, _cacheExpiration);
            }
        }
```

Add `Microsoft.Extensions.Caching.Memory` PackageReference to `src/TelegramBotFlow.Core.Data/TelegramBotFlow.Core.Data.csproj` and `Directory.Packages.props`:

```xml
<PackageVersion Include="Microsoft.Extensions.Caching.Memory" Version="10.0.3" />
```

- [ ] **Step 4: Fix striped lock distribution + configurable timeout (2.2.5 + 2.2.6)**

In `src/TelegramBotFlow.Core/Sessions/InMemorySessionLockProvider.cs`:

Replace hash-based index calculation:
```csharp
        int index = (int)((ulong)userId % STRIPE_COUNT);
```

Make timeout configurable via constructor:
```csharp
    private readonly TimeSpan _lockTimeout;

    public InMemorySessionLockProvider(TimeSpan? lockTimeout = null)
    {
        _lockTimeout = lockTimeout ?? TimeSpan.FromSeconds(10);
        _locks = new SemaphoreSlim[STRIPE_COUNT];
        for (int i = 0; i < STRIPE_COUNT; i++)
            _locks[i] = new SemaphoreSlim(1, 1);
    }
```

- [ ] **Step 5: Fix NavMessageId delete error handling (2.2.7)**

In `src/TelegramBotFlow.Core/Context/UpdateResponder.cs`, find `ReplaceAnchorWithCopyAsync` and update the catch:

```csharp
            catch (ApiRequestException ex) when (ex.ErrorCode is 400 or 403 or 429)
            {
                // 400: message already deleted
                // 403: bot lacks delete permissions
                // 429: rate limited — skip deletion, not critical
            }
```

- [ ] **Step 6: Build and test**

```bash
dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx
```

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "fix: medium bugs — null session, memory leak, lock distribution, error handling

- NavigateBackAsync: guard against null session
- PendingInputMiddleware: log warning when handler not found
- UserTrackingMiddleware: replace ConcurrentDictionary with MemoryCache (1h TTL)
- InMemorySessionLockProvider: use userId modulo for better distribution, configurable timeout
- UpdateResponder: catch 429 rate limit in message deletion"
```

---

### Task 5: Middleware unit tests

Write unit tests for all 5 middleware classes that currently lack coverage.

**Files:**
- Create: `tests/TelegramBotFlow.Core.Tests/Pipeline/ErrorHandlingMiddlewareTests.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Pipeline/PendingInputMiddlewareTests.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Pipeline/WizardMiddlewareTests.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Pipeline/PrivateChatOnlyMiddlewareTests.cs`
- Note: SessionMiddlewareTests already created in Task 1

- [ ] **Step 1: Write ErrorHandlingMiddlewareTests**

Test cases:
- Catches exception and sends error message to user
- Re-throws the exception after notification
- Handles OperationCanceledException without notification
- Handles failure in TryNotifyUser gracefully

- [ ] **Step 2: Write PendingInputMiddlewareTests**

Test cases:
- Command (starts with /) clears pending and passes to next
- Text message with registered handler routes to handler
- Text message with unregistered handler clears pending and falls through
- No pending input passes to next
- Callback query passes to next without checking pending

- [ ] **Step 3: Write WizardMiddlewareTests**

Test cases:
- No active wizard passes to next
- Null session passes to next
- /cancel terminates wizard and passes to next
- nav:menu terminates wizard and passes to next
- Active wizard intercepts text messages
- Missing wizard state clears ActiveWizardId

- [ ] **Step 4: Write PrivateChatOnlyMiddlewareTests**

Test cases:
- Private chat passes to next
- Group chat is blocked (returns without calling next)
- Channel post is blocked
- Update without chat (e.g., inline query) passes to next

- [ ] **Step 5: Run all tests**

```bash
dotnet test TelegramBotFlow.slnx
```

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "test: add middleware unit tests

Coverage for ErrorHandling, PendingInput, Wizard, PrivateChatOnly
middleware classes. ~20 new test cases."
```

---

### Task 6: Error path and edge case tests

Cover error paths, concurrency, and edge cases from the spec.

**Files:**
- Create: `tests/TelegramBotFlow.Core.Tests/Sessions/SessionLockTests.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Screens/NavigationEdgeCaseTests.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Wizards/WizardEdgeCaseTests.cs`

- [ ] **Step 1: Write SessionLockTests**

Test cases:
- Lock timeout throws TimeoutException
- Two concurrent acquires for same stripe — second waits
- Different users get different stripes (no unnecessary contention)

- [ ] **Step 2: Write NavigationEdgeCaseTests**

Test cases:
- Back on empty stack returns null (no-op)
- NavigateToRoot clears stack
- Push beyond MAX_NAVIGATION_DEPTH drops oldest
- PushScreen with same screenId is no-op

- [ ] **Step 3: Write WizardEdgeCaseTests**

Test cases:
- GoBack on first step (empty history) terminates wizard
- State with null fields deserializes correctly
- Invalid step ID in GoTo throws with descriptive message

- [ ] **Step 4: Run all tests**

```bash
dotnet test TelegramBotFlow.slnx
```

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "test: add error path and edge case tests

Session lock concurrency, navigation edge cases, wizard error paths."
```

---

### Task 7: Final verification

- [ ] **Step 1: Full Release build**

```bash
cd "/Users/dev/code/miracle generation/telegram-bot-flow"
dotnet build TelegramBotFlow.slnx -c Release
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 2: Full test run**

```bash
dotnet test TelegramBotFlow.slnx
```

Expected: All tests pass, significantly more tests than before Phase 2.

- [ ] **Step 3: Verify all critical bugs are fixed**

Manually verify:
- `SessionMiddleware`: try-finally pattern with lock scope
- `BotWizard`: try-catch around deserialization, OnEnter snapshot/restore
- `NavigationState.NavigationStack`: returns `IReadOnlyList<string>`

```bash
grep -n "finally" src/TelegramBotFlow.Core/Pipeline/Middlewares/SessionMiddleware.cs
grep -n "IReadOnlyList" src/TelegramBotFlow.Core.Abstractions/Sessions/NavigationState.cs
grep -n "catch (JsonException" src/TelegramBotFlow.Core/Wizards/BotWizard.cs
```

- [ ] **Step 4: Phase 2 complete**

All bugs fixed, tests added. Ready for Phase 3.

# Phase 6: Pipeline & Production — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add UseWhen conditional middleware, webhook health check endpoint, graceful shutdown for update processing, and structured error logging.

**Architecture:** UseWhen wraps a middleware branch in a conditional delegate (ASP.NET Core pattern). Health check is a minimal GET endpoint. Graceful shutdown uses IHostApplicationLifetime to drain in-flight updates. Error logging uses UpdateContext.HandlerName for structured context.

**Tech Stack:** .NET 10, ASP.NET Core, xUnit, FluentAssertions

**Spec:** `docs/specs/2026-04-16-v2-foundation-design.md` sections 6.1–6.4

**Depends on:** Phase 1 completed

---

## File Map

**New files:**
- `tests/TelegramBotFlow.Core.Tests/Pipeline/UseWhenTests.cs`

**Modified files:**
- `src/TelegramBotFlow.Core/Hosting/BotApplicationBuilder.cs` — add UseWhen method
- `src/TelegramBotFlow.Core/Hosting/BotRuntime.cs` — add health check endpoint
- `src/TelegramBotFlow.Core/Hosting/UpdateProcessingWorker.cs` — graceful shutdown
- `src/TelegramBotFlow.Core/Pipeline/Middlewares/ErrorHandlingMiddleware.cs` — structured logging (done in Phase 1 Task 7, verify here)

---

### Task 1: Implement UseWhen conditional middleware

**Files:**
- Modify: `src/TelegramBotFlow.Core/Hosting/BotApplicationBuilder.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Pipeline/UseWhenTests.cs`

- [ ] **Step 1: Write the tests**

```csharp
// tests/TelegramBotFlow.Core.Tests/Pipeline/UseWhenTests.cs
using FluentAssertions;
using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Tests.Pipeline;

public sealed class UseWhenTests
{
    [Fact]
    public async Task UseWhen_PredicateTrue_ExecutesBranch()
    {
        bool branchExecuted = false;

        Func<UpdateDelegate, UpdateDelegate> branchMiddleware = next => async ctx =>
        {
            branchExecuted = true;
            await next(ctx);
        };

        // Build a simple pipeline with UseWhen
        var conditionalMiddleware = ConditionalMiddleware.Create(
            _ => true,
            [branchMiddleware]);

        var pipeline = UpdatePipeline.Build(
            [conditionalMiddleware],
            _ => Task.CompletedTask);

        var ctx = TestHelpers.CreateMessageContext("hello");
        await pipeline.ProcessAsync(ctx);

        branchExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task UseWhen_PredicateFalse_SkipsBranch()
    {
        bool branchExecuted = false;

        Func<UpdateDelegate, UpdateDelegate> branchMiddleware = next => async ctx =>
        {
            branchExecuted = true;
            await next(ctx);
        };

        var conditionalMiddleware = ConditionalMiddleware.Create(
            _ => false,
            [branchMiddleware]);

        var pipeline = UpdatePipeline.Build(
            [conditionalMiddleware],
            _ => Task.CompletedTask);

        var ctx = TestHelpers.CreateMessageContext("hello");
        await pipeline.ProcessAsync(ctx);

        branchExecuted.Should().BeFalse();
    }

    [Fact]
    public async Task UseWhen_PredicateFalse_StillCallsNext()
    {
        bool terminalReached = false;

        var conditionalMiddleware = ConditionalMiddleware.Create(
            _ => false,
            []);

        var pipeline = UpdatePipeline.Build(
            [conditionalMiddleware],
            _ => { terminalReached = true; return Task.CompletedTask; });

        var ctx = TestHelpers.CreateMessageContext("hello");
        await pipeline.ProcessAsync(ctx);

        terminalReached.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run tests — expect failure**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~UseWhenTests"`
Expected: FAIL — `ConditionalMiddleware` not found

- [ ] **Step 3: Create ConditionalMiddleware**

```csharp
// Add to src/TelegramBotFlow.Core/Pipeline/ConditionalMiddleware.cs
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Pipeline;

/// <summary>
/// Wraps a middleware branch in a conditional. If predicate returns true,
/// executes the branch middlewares then continues. If false, skips to next directly.
/// Same pattern as ASP.NET Core IApplicationBuilder.UseWhen.
/// </summary>
internal static class ConditionalMiddleware
{
    public static Func<UpdateDelegate, UpdateDelegate> Create(
        Func<UpdateContext, bool> predicate,
        IReadOnlyList<Func<UpdateDelegate, UpdateDelegate>> branchMiddlewares)
    {
        return next =>
        {
            // Build the branch pipeline that terminates with `next`
            UpdateDelegate branchPipeline = next;
            for (int i = branchMiddlewares.Count - 1; i >= 0; i--)
            {
                branchPipeline = branchMiddlewares[i](branchPipeline);
            }

            return ctx => predicate(ctx) ? branchPipeline(ctx) : next(ctx);
        };
    }
}
```

- [ ] **Step 4: Add UseWhen to BotApplicationBuilder**

```csharp
// In BotApplicationBuilder.cs:
public BotApplicationBuilder UseWhen(
    Func<UpdateContext, bool> predicate,
    Action<BotApplicationBuilder> configureBranch)
{
    var branchBuilder = new BotApplicationBuilder(); // lightweight, just for middleware collection
    configureBranch(branchBuilder);

    _middlewares.Add(ConditionalMiddleware.Create(predicate, branchBuilder._middlewares));
    return this;
}
```

**Note:** This requires a way to create a lightweight `BotApplicationBuilder` just for collecting middleware. May need a private constructor or a separate `MiddlewarePipelineBuilder` class. Decide during implementation — keep it simple.

- [ ] **Step 5: Run tests — expect pass**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~UseWhenTests"`
Expected: All pass

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: add UseWhen conditional middleware for pipeline branching"
```

---

### Task 2: Add health check endpoint

**Files:**
- Modify: `src/TelegramBotFlow.Core/Hosting/BotRuntime.cs`

- [ ] **Step 1: Add health endpoint in BotRuntime**

In `BotRuntime.RunAsync()`, before `_app.RunAsync()`, add:

```csharp
// Health check endpoint — always registered regardless of mode
var config = _app.Services.GetRequiredService<IOptions<BotConfiguration>>().Value;
_app.MapGet(config.HealthCheckPath, () => Results.Ok(new { status = "healthy" }));
```

- [ ] **Step 2: Build and test**

Run: `dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx`
Expected: All pass

- [ ] **Step 3: Commit**

```bash
git add src/TelegramBotFlow.Core/Hosting/BotRuntime.cs
git commit -m "feat: add configurable health check endpoint for production deployments"
```

---

### Task 3: Graceful shutdown for UpdateProcessingWorker

**Files:**
- Modify: `src/TelegramBotFlow.Core/Hosting/UpdateProcessingWorker.cs`

- [ ] **Step 1: Add graceful shutdown logic**

In `UpdateProcessingWorker`, inject `IOptions<BotConfiguration>` for `ShutdownTimeoutSeconds`. Modify `ExecuteAsync`:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("Update processing worker started");

    try
    {
        await Parallel.ForEachAsync(
            _channel.Reader.ReadAllAsync(stoppingToken),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _config.MaxConcurrentUpdates,
                CancellationToken = stoppingToken
            },
            async (update, ct) =>
            {
                // ... existing processing logic
            });
    }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
    {
        _logger.LogInformation("Update processing worker shutting down gracefully");
    }

    _logger.LogInformation("Update processing worker stopped");
}
```

The `Parallel.ForEachAsync` with `CancellationToken` already handles graceful shutdown — when the token is cancelled, it completes current in-flight operations and stops. The channel reader's `ReadAllAsync` completes when the channel is completed (by `PollingService.StopAsync`).

The default .NET hosted service shutdown timeout is controlled by `HostOptions.ShutdownTimeout` (default 30s). We can configure this from `BotConfiguration.ShutdownTimeoutSeconds`:

```csharp
// In ServiceCollectionExtensions:
services.Configure<HostOptions>(opts =>
    opts.ShutdownTimeout = TimeSpan.FromSeconds(config.ShutdownTimeoutSeconds));
```

- [ ] **Step 2: Build and test**

Run: `dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx`
Expected: All pass

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat: configure graceful shutdown timeout for update processing worker"
```

---

### Task 4: Final verification

- [ ] **Step 1: Full build and test**

Run: `dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx`
Expected: All pass, 0 warnings

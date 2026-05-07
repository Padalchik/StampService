using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Pipeline;

/// <summary>
/// Creates a middleware that conditionally executes a branch pipeline based on a predicate.
/// When the predicate returns <c>true</c>, the branch middlewares run before calling <c>next</c>;
/// otherwise, <c>next</c> is called directly.
/// </summary>
internal static class ConditionalMiddleware
{
    /// <summary>
    /// Creates a middleware factory that conditionally applies branch middlewares.
    /// </summary>
    /// <param name="predicate">Predicate evaluated per update to decide whether the branch executes.</param>
    /// <param name="branchMiddlewares">Middleware factories for the conditional branch.</param>
    /// <returns>A middleware factory compatible with the update pipeline.</returns>
    public static Func<UpdateDelegate, UpdateDelegate> Create(
        Func<UpdateContext, bool> predicate,
        IReadOnlyList<Func<UpdateDelegate, UpdateDelegate>> branchMiddlewares)
    {
        return next =>
        {
            UpdateDelegate branchPipeline = next;
            for (int i = branchMiddlewares.Count - 1; i >= 0; i--)
                branchPipeline = branchMiddlewares[i](branchPipeline);

            return ctx => predicate(ctx) ? branchPipeline(ctx) : next(ctx);
        };
    }
}

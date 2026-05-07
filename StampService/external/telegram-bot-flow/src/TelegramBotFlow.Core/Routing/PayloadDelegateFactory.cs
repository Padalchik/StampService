using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Exceptions;
using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Routing;

/// <summary>
/// Фабрика делегатов для action-обработчиков с типизированным payload.
/// Изолирует логику декодирования payload (JSON или short-ID) от остальной инфраструктуры
/// делегатов.
/// </summary>
internal static class PayloadDelegateFactory
{
    /// <summary>
    /// Создаёт <see cref="UpdateDelegate"/> для хэндлера, принимающего типизированный payload.
    /// Callback-data имеет формат <c>{prefix}:{payloadData}</c>, где <c>payloadData</c> —
    /// JSON (<c>j:…</c>) или short-ID ссылки на сессионный payload (<c>s:…</c>).
    /// При истечении сессионного payload отвечает пользователю всплывающим сообщением
    /// и прерывает обработку.
    /// </summary>
    public static UpdateDelegate Create<TPayload>(Delegate handler, string prefix)
    {
        MethodInfo m = handler.Method;
        HandlerDelegateFactory.ValidateHandlerReturnType(m.ReturnType);

        bool payloadConsumed = false;
        ParameterInfo[] parameters = m.GetParameters();
        var resolvers = new Func<UpdateContext, string, object?>[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            Type paramType = parameters[i].ParameterType;

            if (paramType == typeof(UpdateContext))
            {
                resolvers[i] = static (ctx, _) => ctx;
            }
            else if (paramType == typeof(CancellationToken))
            {
                resolvers[i] = static (ctx, _) => ctx.CancellationToken;
            }
            else if (paramType == typeof(TPayload) && !payloadConsumed)
            {
                payloadConsumed = true;
                resolvers[i] = static (ctx, payloadData) =>
                {
                    if (payloadData.StartsWith("j:"))
                    {
                        string json = payloadData[2..];
                        return JsonSerializer.Deserialize<TPayload>(json)!;
                    }

                    if (payloadData.StartsWith("s:"))
                    {
                        string shortId = payloadData[2..];
                        if (ctx.Session is null)
                            throw new PayloadExpiredException();
                        return ctx.Session.Navigation.GetPayload<TPayload>(shortId);
                    }

                    throw new InvalidOperationException($"Invalid payload format: {payloadData}");
                };
            }
            else
            {
                Type serviceType = paramType;
                resolvers[i] = (ctx, _) => ctx.RequestServices.GetRequiredService(serviceType);
            }
        }

        object? target = handler.Target;
        Func<object?, object?[], object?> invoker = CompileInvoker(m);

        return ctx =>
        {
            string payloadData = ctx.CallbackData![(prefix.Length + 1)..];
            return InvokeAndDispatchAsync(invoker, target, resolvers, ctx, payloadData);
        };
    }

    private static async Task InvokeAndDispatchAsync(
        Func<object?, object?[], object?> invoker,
        object? target,
        Func<UpdateContext, string, object?>[] resolvers,
        UpdateContext context,
        string payloadData)
    {
        object?[] args;
        try
        {
            args = new object?[resolvers.Length];
            for (int i = 0; i < resolvers.Length; i++)
                args[i] = resolvers[i](context, payloadData);
        }
        catch (PayloadExpiredException ex)
        {
            var responder = context.RequestServices.GetRequiredService<IUpdateResponder>();
            await responder.AnswerCallbackAsync(context, ex.Message, showAlert: true);
            return;
        }

        IEndpointResult er = await HandlerDelegateFactory.UnwrapResultInternal(invoker(target, args));
        await er.ExecuteAsync(BotExecutionContext.FromUpdateContext(context));
    }

    private static Func<object?, object?[], object?> CompileInvoker(MethodInfo method)
    {
        ParameterExpression target = Expression.Parameter(typeof(object), "target");
        ParameterExpression args = Expression.Parameter(typeof(object[]), "args");

        ParameterInfo[] parameters = method.GetParameters();
        Expression[] callArgs = new Expression[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            callArgs[i] = Expression.Convert(
                Expression.ArrayIndex(args, Expression.Constant(i)),
                parameters[i].ParameterType);
        }

        Expression call = method.IsStatic
            ? Expression.Call(method, callArgs)
            : Expression.Call(Expression.Convert(target, method.DeclaringType!), method, callArgs);

        return Expression.Lambda<Func<object?, object?[], object?>>(
            Expression.Convert(call, typeof(object)), target, args).Compile();
    }
}
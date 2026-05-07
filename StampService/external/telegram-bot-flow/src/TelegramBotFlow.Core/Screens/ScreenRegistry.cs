using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace TelegramBotFlow.Core.Screens;

/// <summary>
/// Реестр экранов с конвенционным вычислением идентификаторов.
/// Логика конвенции вынесена в <see cref="ScreenIdConvention"/>.
/// Атрибут <see cref="ScreenIdAttribute"/> позволяет явно задать идентификатор.
/// </summary>
internal sealed class ScreenRegistry
{
    private readonly Dictionary<string, Type> _screens = [];

    public void Register<TScreen>() where TScreen : class, IScreen =>
        Register(typeof(TScreen));

    public void Register(Type screenType)
    {
        if (!typeof(IScreen).IsAssignableFrom(screenType))
            throw new ArgumentException($"Type {screenType.Name} does not implement IScreen.");

        var attr = screenType.GetCustomAttribute<ScreenIdAttribute>();
        string id = attr?.Id ?? ScreenIdConvention.GetIdFromType(screenType);
        _screens[id] = screenType;
    }

    public void RegisterWithId(string screenId, Type screenType)
    {
        if (!typeof(IScreen).IsAssignableFrom(screenType))
            throw new ArgumentException($"Type {screenType.Name} does not implement IScreen.");

        _screens[screenId] = screenType;
    }

    public IScreen Resolve(string screenId, IServiceProvider services)
    {
        if (!_screens.TryGetValue(screenId, out Type? screenType))
            throw new InvalidOperationException($"Screen '{screenId}' is not registered.");

        return (IScreen)services.GetRequiredService(screenType);
    }

    public bool HasScreen(string screenId) => _screens.ContainsKey(screenId);

    public IReadOnlyCollection<string> GetRegisteredIds() => _screens.Keys;

    // Kept for backward compat — delegates to ScreenIdConvention, respects ScreenIdAttribute
    public static string GetIdFor<TScreen>() where TScreen : class, IScreen =>
        GetIdFromType(typeof(TScreen));

    public static string GetIdFromType(Type screenType)
    {
        var attr = screenType.GetCustomAttribute<ScreenIdAttribute>();
        return attr?.Id ?? ScreenIdConvention.GetIdFromType(screenType);
    }

    internal void RegisterFromAssembly(Assembly assembly)
    {
        foreach (Type screenType in GetScreenTypes(assembly))
            Register(screenType);
    }

    internal static IEnumerable<Type> GetScreenTypes(Assembly assembly) =>
        assembly.DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && t.IsAssignableTo(typeof(IScreen)));
}
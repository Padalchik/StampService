using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// Реестр визардов. Хранит соответствие строкового идентификатора и типа визарда.
/// Аналог <see cref="Screens.ScreenRegistry"/> — Singleton, не создаёт экземпляры визардов.
/// Экземпляр визарда создаётся из DI on-demand в момент обработки апдейта.
/// </summary>
internal sealed class WizardRegistry
{
    private readonly Dictionary<string, Type> _wizards = [];

    /// <summary>
    /// Регистрирует тип визарда. Идентификатор = имя класса.
    /// </summary>
    internal void Register(Type wizardType)
    {
        if (!typeof(IBotWizard).IsAssignableFrom(wizardType))
            throw new ArgumentException($"Type {wizardType.Name} does not implement IBotWizard.");

        _wizards[wizardType.Name] = wizardType;
    }

    /// <summary>
    /// Разрешает экземпляр визарда по идентификатору из DI-контейнера текущего scope.
    /// </summary>
    /// <param name="wizardId">Идентификатор визарда (имя класса).</param>
    /// <param name="services">Провайдер сервисов текущего scope.</param>
    /// <returns>Экземпляр визарда.</returns>
    public IBotWizard Resolve(string wizardId, IServiceProvider services)
    {
        if (!_wizards.TryGetValue(wizardId, out Type? wizardType))
            throw new InvalidOperationException($"Wizard '{wizardId}' is not registered.");

        return (IBotWizard)services.GetRequiredService(wizardType);
    }

    /// <summary>
    /// Проверяет, зарегистрирован ли визард с заданным идентификатором.
    /// </summary>
    public bool HasWizard(string wizardId) => _wizards.ContainsKey(wizardId);

    /// <summary>
    /// Возвращает идентификатор, который будет присвоен типу визарда при регистрации.
    /// По умолчанию — имя класса.
    /// </summary>
    public static string GetIdFor<TWizard>() where TWizard : class, IBotWizard =>
        typeof(TWizard).Name;

    internal void RegisterFromAssembly(Assembly assembly)
    {
        foreach (Type wizardType in GetWizardTypes(assembly))
            Register(wizardType);
    }

    internal static IEnumerable<Type> GetWizardTypes(Assembly assembly) =>
        assembly.DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && t.IsAssignableTo(typeof(IBotWizard)));
}
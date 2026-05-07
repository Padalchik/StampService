using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// Реализация <see cref="IWizardLauncher"/>.
/// Знает о <see cref="WizardRegistry"/> и <see cref="IWizardStore"/> — это его единственная
/// ответственность. <c>StartWizardResult</c> знает только об <c>IWizardLauncher</c>.
/// </summary>
internal sealed class WizardLauncher : IWizardLauncher
{
    private readonly WizardRegistry _registry;
    private readonly IWizardStore _store;

    public WizardLauncher(WizardRegistry registry, IWizardStore store)
    {
        _registry = registry;
        _store = store;
    }

    public async Task<IEndpointResult?> LaunchAsync(Type wizardType, UpdateContext context)
    {
        if (context.Session is null)
            throw new InvalidOperationException("Session is required to start a wizard.");

        string wizardId = wizardType.Name;

        IBotWizard wizard = _registry.Resolve(wizardId, context.RequestServices);
        WizardStorageState storageState = new();

        WizardTransition transition = await wizard.InitializeAsync(context, storageState);

        context.Session.Navigation.ActiveWizardId = wizardId;
        await _store.SaveAsync(context.UserId, wizardId, storageState, context.CancellationToken);

        return transition.EndpointResult;
    }
}
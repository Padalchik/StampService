using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Tests.Wizards;

public sealed class InMemoryWizardStoreTests
{
    private readonly InMemoryWizardStore _store;

    public InMemoryWizardStoreTests()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var config = Options.Create(new BotConfiguration { Token = "test", WizardDefaultTtlMinutes = 60 });
        _store = new InMemoryWizardStore(cache, config);
    }

    [Fact]
    public async Task SaveAndGet_ReturnsState()
    {
        var state = new WizardStorageState { CurrentStepId = "step1", PayloadJson = "{}" };
        await _store.SaveAsync(1, "wiz1", state);
        var result = await _store.GetAsync(1, "wiz1");
        result.Should().NotBeNull();
        result!.CurrentStepId.Should().Be("step1");
    }

    [Fact]
    public async Task Delete_RemovesState()
    {
        var state = new WizardStorageState { CurrentStepId = "step1", PayloadJson = "{}" };
        await _store.SaveAsync(1, "wiz1", state);
        await _store.DeleteAsync(1, "wiz1");
        var result = await _store.GetAsync(1, "wiz1");
        result.Should().BeNull();
    }

    [Fact]
    public async Task Get_NonExistent_ReturnsNull()
    {
        var result = await _store.GetAsync(999, "nonexistent");
        result.Should().BeNull();
    }
}

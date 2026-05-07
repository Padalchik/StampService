using FluentAssertions;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.Core.Tests.Hosting;

public sealed class BotConfigurationTests
{
    [Fact]
    public void Defaults_HaveSensibleValues()
    {
        var config = new BotConfiguration { Token = "test-token" };

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

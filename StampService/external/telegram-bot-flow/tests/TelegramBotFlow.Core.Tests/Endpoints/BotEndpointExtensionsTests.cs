using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.Core.Tests.Endpoints;

public class BotEndpointExtensionsTests
{
    [Fact]
    public void AddBotEndpoints_ShouldRegisterAllEndpointsFromAssembly()
    {
        var services = new ServiceCollection();

        services.AddBotEndpoints(typeof(TestEndpointA).Assembly);

        ServiceProvider provider = services.BuildServiceProvider();
        var endpoints = provider.GetServices<IBotEndpoint>().ToList();

        endpoints.Should().HaveCount(2);
        endpoints.Should().ContainSingle(e => e is TestEndpointA);
        endpoints.Should().ContainSingle(e => e is TestEndpointB);
    }

    [Fact]
    public void AddBotEndpoints_ShouldIgnoreAbstractClasses()
    {
        var services = new ServiceCollection();

        services.AddBotEndpoints(typeof(AbstractEndpoint).Assembly);

        ServiceProvider provider = services.BuildServiceProvider();
        var endpoints = provider.GetServices<IBotEndpoint>().ToList();

        endpoints.Should().NotContain(e => e.GetType() == typeof(AbstractEndpoint));
    }

    [Fact]
    public void AddBotEndpoints_ShouldNotDuplicateOnSecondCall()
    {
        var services = new ServiceCollection();

        services.AddBotEndpoints(typeof(TestEndpointA).Assembly);
        services.AddBotEndpoints(typeof(TestEndpointA).Assembly);

        ServiceProvider provider = services.BuildServiceProvider();
        var endpoints = provider.GetServices<IBotEndpoint>().ToList();

        endpoints.Should().HaveCount(2);
    }
}

public sealed class TestEndpointA : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
    }
}

public sealed class TestEndpointB : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
    }
}

public abstract class AbstractEndpoint : IBotEndpoint
{
    public abstract void MapEndpoint(BotApplication app);
}
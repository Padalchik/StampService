using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.IntegrationTests.Infrastructure;

namespace TelegramBotFlow.IntegrationTests.Application;

/// <summary>
/// Интеграционные тесты регистрации эндпоинтов.
/// </summary>
[Collection(nameof(BotApplicationTests))]
public class EndpointsRegistrationTests : IClassFixture<BotWebApplicationFactory>
{
    private readonly BotWebApplicationFactory _factory;

    public EndpointsRegistrationTests(BotWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Should_Register_Endpoints_From_TelegramBotFlow_App_Assembly()
    {
        // Arrange
        List<Type> expectedEndpointTypes = GetEndpointTypesFromAppAssembly();
        expectedEndpointTypes.Should().NotBeEmpty(
           "TelegramBotFlow.App assembly should contain endpoint implementations");

        // Act
        var registeredEndpoints = _factory.Services.GetServices<IBotEndpoint>().ToList();

        // Assert
        registeredEndpoints.Should().NotBeEmpty("endpoints should be registered");
        registeredEndpoints.Count.Should().BeGreaterThanOrEqualTo(expectedEndpointTypes.Count,
           "all endpoints from App assembly should be registered");
    }

    [Fact]
    public void All_Registered_Endpoints_Should_Implement_IBotEndpoint()
    {
        // Arrange & Act
        IEnumerable<IBotEndpoint> endpoints = _factory.Services.GetServices<IBotEndpoint>();

        // Assert
        foreach (IBotEndpoint endpoint in endpoints)
        {
            endpoint.Should().BeAssignableTo<IBotEndpoint>();
            endpoint.Should().NotBeNull();
        }
    }

    [Fact]
    public void Endpoints_Should_Be_Transient()
    {
        // Arrange & Act
        var endpoints1 = _factory.Services.GetServices<IBotEndpoint>().ToList();
        var endpoints2 = _factory.Services.GetServices<IBotEndpoint>().ToList();

        // Assert
        endpoints1.Should().HaveSameCount(endpoints2);

        for (int i = 0; i < endpoints1.Count && i < endpoints2.Count; i++)
        {
            endpoints1[i].Should().NotBeSameAs(endpoints2[i],
               "endpoints should be transient (new instance each resolution)");
        }
    }

    [Fact]
    public void Each_Endpoint_Should_Have_MapEndpoint_Method()
    {
        // Arrange
        IEnumerable<IBotEndpoint> endpoints = _factory.Services.GetServices<IBotEndpoint>();

        // Act & Assert
        foreach (IBotEndpoint endpoint in endpoints)
        {
            MethodInfo? mapMethod = endpoint.GetType().GetMethod("MapEndpoint");
            mapMethod.Should().NotBeNull(
               $"{endpoint.GetType().Name} should implement MapEndpoint method");

            mapMethod!.GetParameters().Should().HaveCount(1,
               "MapEndpoint should take one parameter");

            mapMethod.GetParameters()[0].ParameterType.Name.Should().Be("BotApplication",
               "MapEndpoint parameter should be BotApplication");
        }
    }

    [Fact]
    public void Should_Discover_All_Concrete_Endpoint_Types_From_App_Assembly()
    {
        // Arrange & Act
        List<Type> endpointTypes = GetEndpointTypesFromAppAssembly();

        // Assert
        endpointTypes.Should().NotBeEmpty();
        endpointTypes.Should().OnlyContain(t =>
           !t.IsAbstract && !t.IsInterface && t.IsClass,
           "all endpoint types should be concrete classes");

        endpointTypes.Should().OnlyContain(t =>
           typeof(IBotEndpoint).IsAssignableFrom(t),
           "all endpoint types should implement IBotEndpoint");
    }

    [Fact]
    public void Should_Resolve_Endpoints_Multiple_Times_Without_Errors()
    {
        // Arrange & Act
        Action action = () =>
        {
            for (int i = 0; i < 50; i++)
            {
                IEnumerable<IBotEndpoint> endpoints = _factory.Services.GetServices<IBotEndpoint>();
                endpoints.Should().NotBeEmpty();
            }
        };

        // Assert
        action.Should().NotThrow(
           "resolving endpoints multiple times should not cause errors");
    }

    [Fact]
    public void Endpoints_Should_Have_Meaningful_Names()
    {
        // Arrange
        IEnumerable<IBotEndpoint> endpoints = _factory.Services.GetServices<IBotEndpoint>();

        // Act & Assert
        foreach (IBotEndpoint endpoint in endpoints)
        {
            string typeName = endpoint.GetType().Name;

            typeName.Should().NotBeNullOrEmpty();
            typeName.Length.Should().BeGreaterThan(3,
               "endpoint names should be meaningful");
        }
    }

    [Fact]
    public void Each_Endpoint_Type_Should_Be_Registered_Only_Once()
    {
        // Arrange
        var endpoints = _factory.Services.GetServices<IBotEndpoint>().ToList();

        // Act
        IEnumerable<IGrouping<Type, IBotEndpoint>> typeGroups = endpoints.GroupBy(e => e.GetType());

        // Assert
        foreach (IGrouping<Type, IBotEndpoint> group in typeGroups)
        {
            group.Should().ContainSingle(
               $"endpoint type {group.Key.Name} should be registered exactly once");
        }
    }

    // Helper

    private static List<Type> GetEndpointTypesFromAppAssembly()
    {
        var appAssembly = Assembly.Load("TelegramBotFlow.App");

        return appAssembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false }
                           && type.IsAssignableTo(typeof(IBotEndpoint)))
            .Cast<Type>()
            .ToList();
    }
}
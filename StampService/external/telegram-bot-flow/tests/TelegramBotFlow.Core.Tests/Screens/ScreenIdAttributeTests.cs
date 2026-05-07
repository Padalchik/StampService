using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Tests.Screens;

public sealed class ScreenIdAttributeTests
{
    [Fact]
    public void Screen_WithScreenIdAttribute_UsesExplicitId()
    {
        var registry = new ScreenRegistry();
        var services = new ServiceCollection();
        services.AddScoped<ExplicitIdScreen>();
        IServiceProvider sp = services.BuildServiceProvider();

        registry.Register<ExplicitIdScreen>();

        registry.HasScreen("custom_screen").Should().BeTrue();
        registry.HasScreen("explicit_id").Should().BeFalse(); // convention id, should NOT be used
    }

    [Fact]
    public void Screen_WithoutScreenIdAttribute_UsesConvention()
    {
        var registry = new ScreenRegistry();
        var services = new ServiceCollection();
        services.AddScoped<ConventionScreen>();
        IServiceProvider sp = services.BuildServiceProvider();

        registry.Register<ConventionScreen>();

        registry.HasScreen("convention").Should().BeTrue();
    }

    [Fact]
    public void GetIdFromType_WithScreenIdAttribute_ReturnsAttributeId()
    {
        string id = ScreenRegistry.GetIdFromType(typeof(ExplicitIdScreen));

        id.Should().Be("custom_screen");
    }

    [Fact]
    public void GetIdFromType_WithoutScreenIdAttribute_ReturnsConventionId()
    {
        string id = ScreenRegistry.GetIdFromType(typeof(ConventionScreen));

        id.Should().Be("convention");
    }

    [Fact]
    public void Screen_WithScreenIdAttribute_CanBeResolved()
    {
        var registry = new ScreenRegistry();
        var services = new ServiceCollection();
        services.AddScoped<ExplicitIdScreen>();
        IServiceProvider sp = services.BuildServiceProvider();

        registry.Register<ExplicitIdScreen>();

        IScreen screen = registry.Resolve("custom_screen", sp);
        screen.Should().BeOfType<ExplicitIdScreen>();
    }

    [ScreenId("custom_screen")]
    private sealed class ExplicitIdScreen : IScreen
    {
        public ValueTask<ScreenView> RenderAsync(UpdateContext ctx) =>
            ValueTask.FromResult(new ScreenView("Explicit"));
    }

    private sealed class ConventionScreen : IScreen
    {
        public ValueTask<ScreenView> RenderAsync(UpdateContext ctx) =>
            ValueTask.FromResult(new ScreenView("Convention"));
    }
}

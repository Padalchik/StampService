using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Tests.Screens;

public sealed class ScreenRegistryTests
{
    [Fact]
    public void Register_And_Resolve_WorkCorrectly()
    {
        var registry = new ScreenRegistry();
        var services = new ServiceCollection();
        services.AddScoped<TestScreen>();
        IServiceProvider sp = services.BuildServiceProvider();

        registry.Register<TestScreen>();

        IScreen screen = registry.Resolve("test", sp);

        screen.Should().NotBeNull();
        screen.Should().BeOfType<TestScreen>();
    }

    [Fact]
    public void Resolve_UnknownScreen_Throws()
    {
        var registry = new ScreenRegistry();
        IServiceProvider sp = new ServiceCollection().BuildServiceProvider();

        Action act = () => registry.Resolve("nonexistent", sp);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*nonexistent*");
    }

    [Fact]
    public void HasScreen_ReturnsTrueForRegistered()
    {
        var registry = new ScreenRegistry();
        registry.Register<TestScreen>();

        registry.HasScreen("test").Should().BeTrue();
        registry.HasScreen("unknown").Should().BeFalse();
    }

    [Fact]
    public void GetRegisteredIds_ReturnsAllIds()
    {
        var registry = new ScreenRegistry();
        registry.Register<TestScreen>();
        registry.RegisterWithId("another", typeof(TestScreen));

        registry.GetRegisteredIds().Should().Contain("test");
        registry.GetRegisteredIds().Should().Contain("another");
    }

    [Fact]
    public void RegisterWithId_UsesProvidedId()
    {
        var registry = new ScreenRegistry();
        registry.RegisterWithId("custom-id", typeof(TestScreen));

        registry.HasScreen("custom-id").Should().BeTrue();
        registry.HasScreen("test").Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(TestScreen), "test")]
    [InlineData(typeof(MainMenuTestScreen), "main_menu_test")]
    [InlineData(typeof(SettingsLangScreen), "settings_lang")]
    [InlineData(typeof(SimpleScreen), "simple")]
    public void GetIdFromType_UsesClassNameConvention(Type screenType, string expectedId)
    {
        ScreenRegistry.GetIdFromType(screenType).Should().Be(expectedId);
    }

    [Fact]
    public void Register_UsesClassNameConvention()
    {
        var registry = new ScreenRegistry();
        registry.Register<MainMenuTestScreen>();

        registry.HasScreen("main_menu_test").Should().BeTrue();
    }

    private sealed class TestScreen : IScreen
    {
        public ValueTask<ScreenView> RenderAsync(UpdateContext ctx) =>
            ValueTask.FromResult(new ScreenView("Test screen"));
    }

    private sealed class MainMenuTestScreen : IScreen
    {
        public ValueTask<ScreenView> RenderAsync(UpdateContext ctx) =>
            ValueTask.FromResult(new ScreenView("Main menu test screen"));
    }

    private sealed class SettingsLangScreen : IScreen
    {
        public ValueTask<ScreenView> RenderAsync(UpdateContext ctx) =>
            ValueTask.FromResult(new ScreenView("Settings lang screen"));
    }

    private sealed class SimpleScreen : IScreen
    {
        public ValueTask<ScreenView> RenderAsync(UpdateContext ctx) =>
            ValueTask.FromResult(new ScreenView("Simple"));
    }
}
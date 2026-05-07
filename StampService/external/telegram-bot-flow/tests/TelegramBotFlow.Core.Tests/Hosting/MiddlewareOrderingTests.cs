using FluentAssertions;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.Core.Tests.Hosting;

/// <summary>
/// Tests that middleware ordering validation catches incorrect registration order.
/// Uses MiddlewareOrderValidator directly (internal, visible via InternalsVisibleTo).
/// </summary>
public sealed class MiddlewareOrderingTests
{
    [Fact]
    public void UseWizards_WithoutSession_Throws()
    {
        var middleware = new List<string> { "wizards" };

        Action act = () => MiddlewareOrderValidator.Validate(middleware);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*UseSessions()*");
    }

    [Fact]
    public void UsePendingInput_WithoutSession_Throws()
    {
        var middleware = new List<string> { "pending_input" };

        Action act = () => MiddlewareOrderValidator.Validate(middleware);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*UseSessions()*");
    }

    [Fact]
    public void UseWizards_AfterSession_Succeeds()
    {
        var middleware = new List<string> { "session", "wizards" };

        Action act = () => MiddlewareOrderValidator.Validate(middleware);

        act.Should().NotThrow();
    }

    [Fact]
    public void UsePendingInput_AfterSession_Succeeds()
    {
        var middleware = new List<string> { "session", "pending_input" };

        Action act = () => MiddlewareOrderValidator.Validate(middleware);

        act.Should().NotThrow();
    }

    [Fact]
    public void UseWizards_BeforeSession_Throws()
    {
        var middleware = new List<string> { "wizards", "session" };

        Action act = () => MiddlewareOrderValidator.Validate(middleware);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*UseSessions()*");
    }

    [Fact]
    public void NoSessionDependentMiddlewares_WithoutSession_Succeeds()
    {
        var middleware = new List<string> { "error_handling" };

        Action act = () => MiddlewareOrderValidator.Validate(middleware);

        act.Should().NotThrow();
    }

    [Fact]
    public void EmptyMiddlewareList_Succeeds()
    {
        var middleware = new List<string>();

        Action act = () => MiddlewareOrderValidator.Validate(middleware);

        act.Should().NotThrow();
    }

    [Fact]
    public void FullCorrectOrder_Succeeds()
    {
        var middleware = new List<string> { "error_handling", "session", "wizards", "pending_input" };

        Action act = () => MiddlewareOrderValidator.Validate(middleware);

        act.Should().NotThrow();
    }
}

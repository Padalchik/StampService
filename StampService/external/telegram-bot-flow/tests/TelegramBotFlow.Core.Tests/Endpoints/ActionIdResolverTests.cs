using FluentAssertions;
using TelegramBotFlow.Core.Endpoints;

namespace TelegramBotFlow.Core.Tests.Endpoints;

public sealed class ActionIdResolverTests
{
    [Fact]
    public void GetId_ActionWithActionIdAttribute_ReturnsAttributeId()
    {
        string id = ActionIdResolver.GetId<ExplicitAction>();

        id.Should().Be("my_custom_action");
    }

    [Fact]
    public void GetId_ActionWithoutAttribute_ReturnsTypeName()
    {
        string id = ActionIdResolver.GetId<PlainAction>();

        id.Should().Be("PlainAction");
    }

    [Fact]
    public void GetId_ByType_WithAttribute_ReturnsAttributeId()
    {
        string id = ActionIdResolver.GetId(typeof(ExplicitAction));

        id.Should().Be("my_custom_action");
    }

    [Fact]
    public void GetId_ByType_WithoutAttribute_ReturnsTypeName()
    {
        string id = ActionIdResolver.GetId(typeof(PlainAction));

        id.Should().Be("PlainAction");
    }

    [ActionId("my_custom_action")]
    private sealed class ExplicitAction : IBotAction;

    private sealed class PlainAction : IBotAction;
}

using FluentAssertions;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Tests.UI;

public sealed class TelegramHtmlTests
{
    // ── Escape ────────────────────────────────────────────────────────────────

    [Fact]
    public void Escape_ReplacesAmpersand()
    {
        TelegramHtml.Escape("foo & bar").Should().Be("foo &amp; bar");
    }

    [Fact]
    public void Escape_ReplacesLessThan()
    {
        TelegramHtml.Escape("<b>bold</b>").Should().Be("&lt;b&gt;bold&lt;/b&gt;");
    }

    [Fact]
    public void Escape_ReplacesGreaterThan()
    {
        TelegramHtml.Escape("a > b").Should().Be("a &gt; b");
    }

    [Fact]
    public void Escape_MultipleSpecialChars_ReplacesAll()
    {
        TelegramHtml.Escape("a & b < c > d").Should().Be("a &amp; b &lt; c &gt; d");
    }

    [Fact]
    public void Escape_NoSpecialChars_ReturnsOriginal()
    {
        TelegramHtml.Escape("Hello, World!").Should().Be("Hello, World!");
    }

    [Fact]
    public void Escape_EmptyString_ReturnsEmpty()
    {
        TelegramHtml.Escape(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Escape_AmpersandInAmpersandEntity_EscapesCorrectly()
    {
        // Двойное экранирование: & в уже-экранированной строке
        TelegramHtml.Escape("&amp;").Should().Be("&amp;amp;");
    }

    // ── Bold ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Bold_WrapsTextInBoldTag()
    {
        TelegramHtml.Bold("hello").Should().Be("<b>hello</b>");
    }

    [Fact]
    public void Bold_EscapesContentBeforeWrapping()
    {
        TelegramHtml.Bold("a < b").Should().Be("<b>a &lt; b</b>");
    }

    // ── Italic ────────────────────────────────────────────────────────────────

    [Fact]
    public void Italic_WrapsTextInItalicTag()
    {
        TelegramHtml.Italic("hello").Should().Be("<i>hello</i>");
    }

    [Fact]
    public void Italic_EscapesContentBeforeWrapping()
    {
        TelegramHtml.Italic("a & b").Should().Be("<i>a &amp; b</i>");
    }

    // ── Code ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Code_WrapsTextInCodeTag()
    {
        TelegramHtml.Code("console.log()").Should().Be("<code>console.log()</code>");
    }

    [Fact]
    public void Code_EscapesAngleBrackets()
    {
        TelegramHtml.Code("List<T>").Should().Be("<code>List&lt;T&gt;</code>");
    }

    // ── Composition ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("&", "&amp;")]
    [InlineData("<", "&lt;")]
    [InlineData(">", "&gt;")]
    [InlineData("", "")]
    [InlineData("safe text", "safe text")]
    public void Escape_Theory(string input, string expected)
    {
        TelegramHtml.Escape(input).Should().Be(expected);
    }
}
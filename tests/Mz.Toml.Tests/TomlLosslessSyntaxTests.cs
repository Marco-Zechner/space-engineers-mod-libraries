using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlLosslessSyntaxTests
{
    [Fact]
    public void SourceSpan_Represents_Half_Open_Character_Range()
    {
        var span = new TomlSourceSpan(4, 3);

        Assert.Equal(4, span.Start);
        Assert.Equal(3, span.Length);
        Assert.Equal(7, span.End);
        Assert.True(span.Contains(4));
        Assert.True(span.Contains(6));
        Assert.False(span.Contains(7));
        Assert.Equal("[4..7)", span.ToString());
        Assert.Equal(new TomlSourceSpan(4, 3), span);
    }

    [Theory]
    [InlineData(-1, 0, "start")]
    [InlineData(0, -1, "length")]
    public void SourceSpan_Rejects_Negative_Values(int start, int length, string parameter)
    {
        var exception = Assert.Throws<ArgumentException>(() => new TomlSourceSpan(start, length));

        Assert.Equal(parameter, exception.ParamName);
    }

    [Fact]
    public void SourceSpan_Rejects_Overflowing_End()
    {
        var exception = Assert.Throws<ArgumentException>(() => new TomlSourceSpan(int.MaxValue, 1));

        Assert.Equal("length", exception.ParamName);
    }

    [Fact]
    public void Successful_Parse_Preserves_Exact_Top_Level_Source_Layout()
    {
        const string source =
            "# heading\r\n" +
            "  title = \"Example\"  # player note\r\n" +
            "\r\n" +
            "[server]\r\n" +
            "enabled = true";

        var result = Toml.TryParse(source);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Syntax);
        Assert.Equal(source, result.Syntax.Source);

        Assert.Equal(
        [
            TomlSyntaxNodeKind.Comment,
            TomlSyntaxNodeKind.Newline,
            TomlSyntaxNodeKind.Whitespace,
            TomlSyntaxNodeKind.Assignment,
            TomlSyntaxNodeKind.Whitespace,
            TomlSyntaxNodeKind.Comment,
            TomlSyntaxNodeKind.Newline,
            TomlSyntaxNodeKind.Newline,
            TomlSyntaxNodeKind.TableHeader,
            TomlSyntaxNodeKind.Newline,
            TomlSyntaxNodeKind.Assignment
        ],
            result.Syntax.Nodes.Select(node => node.Kind)
        );

        var reconstructed = string.Concat(result.Syntax.Nodes.Select(node => source.Substring(node.Span.Start, node.Span.Length)));
        Assert.Equal(source, reconstructed);

        Assert.Equal("# heading", TextOf(result.Syntax, 0));
        Assert.Equal("\r\n", TextOf(result.Syntax, 1));
        Assert.Equal("  ", TextOf(result.Syntax, 2));
        Assert.Equal("title = \"Example\"", TextOf(result.Syntax, 3));
        Assert.Equal("  ", TextOf(result.Syntax, 4));
        Assert.Equal("# player note", TextOf(result.Syntax, 5));
        Assert.Equal("[server]", TextOf(result.Syntax, 8));
        Assert.Equal("enabled = true", TextOf(result.Syntax, 10));
    }

    [Fact]
    public void Syntax_Nodes_Expose_Exact_Ordered_Spans()
    {
        const string source = "a = 1\nb = 2\n";

        var syntax = Toml.TryParse(source).Syntax;

        Assert.Equal(new TomlSourceSpan(0, 5), syntax.Nodes[0].Span);
        Assert.Equal(new TomlSourceSpan(5, 1), syntax.Nodes[1].Span);
        Assert.Equal(new TomlSourceSpan(6, 5), syntax.Nodes[2].Span);
        Assert.Equal(new TomlSourceSpan(11, 1), syntax.Nodes[3].Span);

        for (var index = 1; index < syntax.Nodes.Count; index++)
            Assert.Equal(syntax.Nodes[index - 1].Span.End, syntax.Nodes[index].Span.Start);
    }

    [Fact]
    public void Empty_Source_Produces_Empty_Lossless_Syntax()
    {
        var result = Toml.TryParse(string.Empty);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Syntax);
        Assert.Equal(string.Empty, result.Syntax.Source);
        Assert.Empty(result.Syntax.Nodes);
    }

    [Fact]
    public void Syntax_Nodes_Are_ReadOnly()
    {
        var syntax = Toml.TryParse("value = 1\n").Syntax;

        Assert.False(syntax.Nodes is ICollection<TomlSyntaxNode>);
    }
    private static string TextOf(TomlSyntaxDocument syntax, int index)
    {
        var span = syntax.Nodes[index].Span;
        return syntax.Source.Substring(span.Start, span.Length);
    }
}
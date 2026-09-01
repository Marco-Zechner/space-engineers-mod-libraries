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

    [Fact]
    public void Trivia_Exposes_Top_Level_Comments_Whitespace_And_Newlines()
    {
        const string source =
            "# heading\r\n" +
            "  value = 1  # note\r\n";

        var syntax = Toml.TryParse(source).Syntax;

        Assert.Equal(
        [
            TomlSyntaxTriviaKind.Comment,
            TomlSyntaxTriviaKind.Newline,
            TomlSyntaxTriviaKind.Whitespace,
            TomlSyntaxTriviaKind.Whitespace,
            TomlSyntaxTriviaKind.Whitespace,
            TomlSyntaxTriviaKind.Whitespace,
            TomlSyntaxTriviaKind.Comment,
            TomlSyntaxTriviaKind.Newline
        ],
            syntax.Trivia.Select(trivia => trivia.Kind)
        );

        Assert.Equal("# heading", TriviaTextOf(syntax, 0));
        Assert.Equal("\r\n", TriviaTextOf(syntax, 1));
        Assert.Equal("  ", TriviaTextOf(syntax, 2));
        Assert.Equal(" ", TriviaTextOf(syntax, 3));
        Assert.Equal(" ", TriviaTextOf(syntax, 4));
        Assert.Equal("  ", TriviaTextOf(syntax, 5));
        Assert.Equal("# note", TriviaTextOf(syntax, 6));
        Assert.Equal("\r\n", TriviaTextOf(syntax, 7));
    }

    [Fact]
    public void Trivia_Exposes_Comments_And_Layout_Inside_Multiline_Array()
    {
        const string source =
            "values = [\r\n" +
            "  1, # first\r\n" +
            "  # between\r\n" +
            "  2,\r\n" +
            "]\r\n";

        var syntax = Toml.TryParse(source).Syntax;

        Assert.Equal(
        [
            " ",
            " ",
            "\r\n",
            "  ",
            " ",
            "# first",
            "\r\n",
            "  ",
            "# between",
            "\r\n",
            "  ",
            "\r\n",
            "\r\n"
        ],
            syntax.Trivia.Select(trivia => source.Substring(trivia.Span.Start, trivia.Span.Length))
        );

        Assert.Equal(
            2,
            syntax.Trivia.Count(trivia => trivia.Kind == TomlSyntaxTriviaKind.Comment)
        );

        Assert.Contains(
            syntax.Trivia,
            trivia => trivia.Kind == TomlSyntaxTriviaKind.Comment && TriviaTextOf(syntax, trivia) == "# first"
        );

        Assert.Contains(
            syntax.Trivia,
            trivia => trivia.Kind == TomlSyntaxTriviaKind.Comment && TriviaTextOf(syntax, trivia) == "# between"
        );
    }

    [Fact]
    public void Array_Trivia_Does_Not_Split_Top_Level_Assignment_Node()
    {
        const string source =
            "values = [\n" +
            "  1, # first\n" +
            "  2\n" +
            "]\n";

        var syntax = Toml.TryParse(source).Syntax;

        Assert.Equal(TomlSyntaxNodeKind.Assignment, syntax.Nodes[0].Kind);
        Assert.Equal(source.TrimEnd('\n'), TextOf(syntax, 0));
        Assert.Equal(TomlSyntaxNodeKind.Newline, syntax.Nodes[1].Kind);
        Assert.Equal(source, string.Concat(syntax.Nodes.Select(node => TextOf(syntax, node))));
    }

    [Fact]
    public void Multiline_String_Content_Newlines_Are_Not_Trivia()
    {
        const string source =
            "message = \"\"\"\r\n" +
            "line one\r\n" +
            "line two\"\"\"\r\n";

        var syntax = Toml.TryParse(source).Syntax;

        Assert.Equal(
        [
            " ",
            " ",
            "\r\n"
        ],
            syntax.Trivia.Select(trivia => TriviaTextOf(syntax, trivia))
        );
    }

    [Fact]
    public void Trivia_Exposes_Whitespace_Inside_Dotted_Keys_And_Inline_Tables()
    {
        const string source = "a . b =   { x = 1, y = 2 }\n";

        var syntax = Toml.TryParse(source).Syntax;

        Assert.Equal(
        [
            " ",
            " ",
            " ",
            "   ",
            " ",
            " ",
            " ",
            " ",
            " ",
            " ",
            " ",
            "\n"
        ],
            syntax.Trivia.Select(trivia => TriviaTextOf(syntax, trivia))
        );
    }

    [Fact]
    public void String_Content_Whitespace_Is_Not_Trivia()
    {
        const string source = "name = \"hello world\"\n";

        var syntax = Toml.TryParse(source).Syntax;

        Assert.Equal(
        [
            " ",
            " ",
            "\n"
        ],
            syntax.Trivia.Select(trivia => TriviaTextOf(syntax, trivia))
        );
    }
    [Fact]
    public void Trivia_Spans_Are_Source_Ordered_And_Do_Not_Overlap()
    {
        const string source =
            "values = [\r\n" +
            "  1, # first\r\n" +
            "  2\r\n" +
            "] # after\r\n";

        var trivia = Toml.TryParse(source).Syntax.Trivia;

        for (var index = 1; index < trivia.Count; index++)
            Assert.True(trivia[index - 1].Span.End <= trivia[index].Span.Start);
    }
    [Fact]
    public void Trivia_Is_ReadOnly()
    {
        var syntax = Toml.TryParse("# comment\nvalue = 1\n").Syntax;

        Assert.False(syntax.Trivia is ICollection<TomlSyntaxTrivia>);
    }

    private static string TextOf(TomlSyntaxDocument syntax, TomlSyntaxNode node)
        => syntax.Source.Substring(node.Span.Start, node.Span.Length);

    private static string TriviaTextOf(TomlSyntaxDocument syntax, int index)
        => TriviaTextOf(syntax, syntax.Trivia[index]);

    private static string TriviaTextOf(TomlSyntaxDocument syntax, TomlSyntaxTrivia trivia)
        => syntax.Source.Substring(trivia.Span.Start, trivia.Span.Length);

    private static string TextOf(TomlSyntaxDocument syntax, int index)
    {
        var span = syntax.Nodes[index].Span;
        return syntax.Source.Substring(span.Start, span.Length);
    }
}
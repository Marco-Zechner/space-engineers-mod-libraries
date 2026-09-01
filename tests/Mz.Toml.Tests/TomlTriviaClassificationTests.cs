using System.Linq;
using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlTriviaClassificationTests
{
    [Fact]
    public void Standalone_Comment_And_Indentation_Are_TopLevel()
    {
        const string source =
            "  # description\r\n" +
            "value = 1\r\n";

        var syntax = Toml.TryParse(source).Syntax;

        Assert.Equal(
        [
            ("  ", TomlSyntaxTriviaKind.Whitespace, TomlSyntaxTriviaPlacement.TopLevel),
            ("# description", TomlSyntaxTriviaKind.Comment, TomlSyntaxTriviaPlacement.TopLevel),
            ("\r\n", TomlSyntaxTriviaKind.Newline, TomlSyntaxTriviaPlacement.TopLevel),
            (" ", TomlSyntaxTriviaKind.Whitespace, TomlSyntaxTriviaPlacement.WithinStatement),
            (" ", TomlSyntaxTriviaKind.Whitespace, TomlSyntaxTriviaPlacement.WithinStatement),
            ("\r\n", TomlSyntaxTriviaKind.Newline, TomlSyntaxTriviaPlacement.TopLevel)
        ],
            syntax.Trivia.Select(trivia =>
                (TextOf(syntax, trivia), trivia.Kind, trivia.Placement))
        );
    }

    [Fact]
    public void Active_Assignment_Trailing_Whitespace_And_Comment_Are_Trailing()
    {
        const string source = "value = 1  # note\r\n";

        var syntax = Toml.TryParse(source).Syntax;

        Assert.Equal(
        [
            (" ", TomlSyntaxTriviaPlacement.WithinStatement),
            (" ", TomlSyntaxTriviaPlacement.WithinStatement),
            ("  ", TomlSyntaxTriviaPlacement.Trailing),
            ("# note", TomlSyntaxTriviaPlacement.Trailing),
            ("\r\n", TomlSyntaxTriviaPlacement.TopLevel)
        ],
            syntax.Trivia.Select(trivia => (TextOf(syntax, trivia), trivia.Placement))
        );
    }

    [Fact]
    public void Disabled_Assignment_Internal_And_Trailing_Trivia_Are_Distinct()
    {
        const string source = "#! value = [ 1, 2 ]  # disabled\n";

        var syntax = Toml.TryParse(source).Syntax;

        Assert.Contains(
            syntax.Trivia,
            trivia =>
                TextOf(syntax, trivia) == " " &&
                trivia.Span.Start == 2 &&
                trivia.Placement == TomlSyntaxTriviaPlacement.WithinStatement
        );

        Assert.Contains(
            syntax.Trivia,
            trivia =>
                TextOf(syntax, trivia) == "  " &&
                trivia.Placement == TomlSyntaxTriviaPlacement.Trailing
        );

        Assert.Contains(
            syntax.Trivia,
            trivia =>
                TextOf(syntax, trivia) == "# disabled" &&
                trivia.Placement == TomlSyntaxTriviaPlacement.Trailing
        );
    }

    [Fact]
    public void Header_Trailing_Comment_Is_Trailing_But_Following_Newline_Is_TopLevel()
    {
        const string source =
            "[server]   # settings\n" +
            "enabled = true\n";

        var syntax = Toml.TryParse(source).Syntax;
        var comment = syntax.Trivia.Single(trivia =>
            trivia.Kind == TomlSyntaxTriviaKind.Comment);

        Assert.Equal("# settings", TextOf(syntax, comment));
        Assert.Equal(TomlSyntaxTriviaPlacement.Trailing, comment.Placement);

        var newlineAfterComment = syntax.Trivia.First(trivia =>
            trivia.Kind == TomlSyntaxTriviaKind.Newline &&
            trivia.Span.Start > comment.Span.End);

        Assert.Equal(TomlSyntaxTriviaPlacement.TopLevel, newlineAfterComment.Placement);
    }

    [Fact]
    public void Array_Whitespace_Newlines_And_Comments_Are_WithinStatement()
    {
        const string source =
            "values = [\r\n" +
            "  1, # first\r\n" +
            "  # between\r\n" +
            "  2,\r\n" +
            "]\r\n";

        var syntax = Toml.TryParse(source).Syntax;
        var assignment = syntax.Nodes.Single(node =>
            node.Kind == TomlSyntaxNodeKind.Assignment);

        var nested = syntax.Trivia
            .Where(trivia =>
                trivia.Span.Start >= assignment.Span.Start &&
                trivia.Span.End <= assignment.Span.End)
            .ToArray();

        Assert.NotEmpty(nested);
        Assert.All(
            nested,
            trivia => Assert.Equal(
                TomlSyntaxTriviaPlacement.WithinStatement,
                trivia.Placement)
        );

        Assert.Contains(
            nested,
            trivia =>
                trivia.Kind == TomlSyntaxTriviaKind.Comment &&
                TextOf(syntax, trivia) == "# first"
        );

        Assert.Contains(
            nested,
            trivia =>
                trivia.Kind == TomlSyntaxTriviaKind.Comment &&
                TextOf(syntax, trivia) == "# between"
        );

        var finalNewline = syntax.Trivia.Last();

        Assert.Equal("\r\n", TextOf(syntax, finalNewline));
        Assert.Equal(TomlSyntaxTriviaPlacement.TopLevel, finalNewline.Placement);
    }

    [Fact]
    public void Structural_Whitespace_In_Dotted_Keys_And_Inline_Tables_Is_WithinStatement()
    {
        const string source = "a . b =   { x = 1, y = 2 }  # tail\n";

        var syntax = Toml.TryParse(source).Syntax;
        var assignment = syntax.Nodes.Single(node =>
            node.Kind == TomlSyntaxNodeKind.Assignment);

        Assert.All(
            syntax.Trivia.Where(trivia =>
                trivia.Span.Start >= assignment.Span.Start &&
                trivia.Span.End <= assignment.Span.End),
            trivia => Assert.Equal(
                TomlSyntaxTriviaPlacement.WithinStatement,
                trivia.Placement)
        );

        var trailingComment = syntax.Trivia.Single(trivia =>
            trivia.Kind == TomlSyntaxTriviaKind.Comment);

        Assert.Equal(TomlSyntaxTriviaPlacement.Trailing, trailingComment.Placement);
    }

    [Fact]
    public void Comment_After_Blank_Line_Remains_TopLevel_Without_Field_Ownership()
    {
        const string source =
            "first = 1\n" +
            "\n" +
            "# maybe about second\n" +
            "second = 2\n";

        var syntax = Toml.TryParse(source).Syntax;
        var comment = syntax.Trivia.Single(trivia =>
            trivia.Kind == TomlSyntaxTriviaKind.Comment);

        Assert.Equal(TomlSyntaxTriviaPlacement.TopLevel, comment.Placement);
    }

    [Fact]
    public void Standalone_Comment_At_End_Of_File_Is_TopLevel()
    {
        const string source =
            "value = 1\n" +
            "# eof comment";

        var syntax = Toml.TryParse(source).Syntax;
        var comment = syntax.Trivia.Single(trivia =>
            trivia.Kind == TomlSyntaxTriviaKind.Comment);

        Assert.Equal(TomlSyntaxTriviaPlacement.TopLevel, comment.Placement);
    }

    [Fact]
    public void Multiline_String_Content_Still_Does_Not_Create_Trivia()
    {
        const string source =
            "message = \"\"\"\n" +
            "line one\n" +
            "line two\"\"\"\n";

        var syntax = Toml.TryParse(source).Syntax;

        Assert.Equal(3, syntax.Trivia.Count);
        Assert.All(
            syntax.Trivia.Take(2),
            trivia => Assert.Equal(
                TomlSyntaxTriviaPlacement.WithinStatement,
                trivia.Placement)
        );

        Assert.Equal(TomlSyntaxTriviaPlacement.TopLevel, syntax.Trivia.Last().Placement);
    }

    private static string TextOf(TomlSyntaxDocument syntax, TomlSyntaxTrivia trivia)
        => syntax.Source.Substring(trivia.Span.Start, trivia.Span.Length);
}

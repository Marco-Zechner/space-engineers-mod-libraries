using System;
using System.Linq;
using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlInvalidSourceSyntaxTests
{
    [Fact]
    public void Failed_Decoded_Parse_Preserves_Exact_Source_With_Unparsed_Remainder()
    {
        const string source =
            "first = 1\r\n" +
            "broken = [1,\r\n" +
            "next = 2\r\n";

        var result = Toml.TryParse(source);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Document);
        Assert.NotNull(result.Syntax);
        Assert.Equal(source, result.Syntax.Source);

        Assert.Equal(
        [
            TomlSyntaxNodeKind.Assignment,
            TomlSyntaxNodeKind.Newline,
            TomlSyntaxNodeKind.Unparsed
        ],
            result.Syntax.Nodes.Select(node => node.Kind)
        );

        Assert.Equal(
            "broken = [1,\r\n" +
            "next = 2\r\n",
            TextOf(result.Syntax, result.Syntax.Nodes[2])
        );

        Assert.Equal(source, Reconstruct(result.Syntax));
    }

    [Fact]
    public void Fully_Recognized_Semantic_Failure_Does_Not_Need_Unparsed_Syntax()
    {
        const string source =
            "value = 1\n" +
            "value = 2\n";

        var result = Toml.TryParse(source);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Document);
        Assert.Equal(TomlDiagnosticCode.DuplicateKey, Assert.Single(result.Diagnostics).Code);

        Assert.NotNull(result.Syntax);
        Assert.Equal(source, Reconstruct(result.Syntax));

        Assert.Equal(
        [
            TomlSyntaxNodeKind.Assignment,
            TomlSyntaxNodeKind.Newline,
            TomlSyntaxNodeKind.Assignment,
            TomlSyntaxNodeKind.Newline
        ],
            result.Syntax.Nodes.Select(node => node.Kind)
        );

        Assert.DoesNotContain(
            result.Syntax.Nodes,
            node => node.Kind == TomlSyntaxNodeKind.Unparsed
        );
    }

    [Fact]
    public void Invalid_Trailing_Comment_Preserves_Completed_Assignment_And_Unparsed_Remainder()
    {
        var source =
            "value = 1 # bad" + '\u007F' + "\n" +
            "next = 2\n";

        var result = Toml.TryParse(source);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Document);
        Assert.Equal(TomlDiagnosticCode.InvalidComment, Assert.Single(result.Diagnostics).Code);

        Assert.NotNull(result.Syntax);

        Assert.Equal(
        [
            TomlSyntaxNodeKind.Assignment,
            TomlSyntaxNodeKind.Whitespace,
            TomlSyntaxNodeKind.Unparsed
        ],
            result.Syntax.Nodes.Select(node => node.Kind)
        );

        Assert.Equal(
            "# bad" + '\u007F' + "\n" +
            "next = 2\n",
            TextOf(result.Syntax, result.Syntax.Nodes[2])
        );

        Assert.Equal(source, Reconstruct(result.Syntax));
    }

    [Fact]
    public void Malformed_Disabled_Assignment_Preserves_Extension_Diagnostic_And_Unparsed_Source()
    {
        const string source =
            "  #! value =\n" +
            "next = true\n";

        var result = Toml.TryParse(source);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Document);

        var diagnostic = Assert.Single(result.Diagnostics);

        Assert.Equal(TomlDiagnosticCode.InvalidDisabledAssignment, diagnostic.Code);
        Assert.Equal(1, diagnostic.Line);
        Assert.Equal(13, diagnostic.Column);

        Assert.NotNull(result.Syntax);

        Assert.Equal(
        [
            TomlSyntaxNodeKind.Whitespace,
            TomlSyntaxNodeKind.Unparsed
        ],
            result.Syntax.Nodes.Select(node => node.Kind)
        );

        Assert.Equal(
            "#! value =\n" +
            "next = true\n",
            TextOf(result.Syntax, result.Syntax.Nodes[1])
        );

        Assert.Equal(source, Reconstruct(result.Syntax));
    }

    [Fact]
    public void Malformed_Header_Before_Syntax_Emission_Preserves_Recognized_Prefix()
    {
        const string source =
            "# keep\r\n" +
            "[broken\r\n" +
            "next = 1\r\n";

        var result = Toml.TryParse(source);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Document);
        Assert.Equal(TomlDiagnosticCode.InvalidTable, Assert.Single(result.Diagnostics).Code);

        Assert.NotNull(result.Syntax);

        Assert.Equal(
        [
            TomlSyntaxNodeKind.Comment,
            TomlSyntaxNodeKind.Newline,
            TomlSyntaxNodeKind.Unparsed
        ],
            result.Syntax.Nodes.Select(node => node.Kind)
        );

        Assert.Equal(
            "[broken\r\n" +
            "next = 1\r\n",
            TextOf(result.Syntax, result.Syntax.Nodes[2])
        );

        Assert.Equal(source, Reconstruct(result.Syntax));
    }

    [Fact]
    public void Safely_Recognized_Trivia_May_Remain_Classified_Inside_Unparsed_Statement()
    {
        const string source = "value =   # missing\n";

        var result = Toml.TryParse(source);

        Assert.False(result.IsSuccess);
        Assert.Equal(TomlDiagnosticCode.MissingValue, Assert.Single(result.Diagnostics).Code);

        Assert.NotNull(result.Syntax);
        Assert.Single(result.Syntax.Nodes);
        Assert.Equal(TomlSyntaxNodeKind.Unparsed, result.Syntax.Nodes[0].Kind);
        Assert.Equal(source, TextOf(result.Syntax, result.Syntax.Nodes[0]));

        Assert.Contains(
            result.Syntax.Trivia,
            trivia =>
                trivia.Kind == TomlSyntaxTriviaKind.Whitespace &&
                trivia.Placement == TomlSyntaxTriviaPlacement.WithinStatement &&
                TextOf(result.Syntax, trivia) == "   "
        );

        Assert.Equal(source, Reconstruct(result.Syntax));
    }

    [Fact]
    public void Successful_Parse_Never_Produces_Unparsed_Syntax()
    {
        const string source =
            "# comment\n" +
            "value = [1, 2]\n";

        var result = Toml.TryParse(source);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Document);
        Assert.NotNull(result.Syntax);

        Assert.DoesNotContain(
            result.Syntax.Nodes,
            node => node.Kind == TomlSyntaxNodeKind.Unparsed
        );

        Assert.Equal(source, Reconstruct(result.Syntax));
    }

    [Fact]
    public void Invalid_Utf8_Has_No_Syntax_Because_No_Trusted_Source_String_Exists()
    {
        byte[] source = [0xC3];

        var result = Toml.TryParse(source);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Document);
        Assert.Null(result.Syntax);
        Assert.Equal(TomlDiagnosticCode.InvalidEncoding, Assert.Single(result.Diagnostics).Code);
    }

    private static string Reconstruct(TomlSyntaxDocument syntax)
        => string.Concat(syntax.Nodes.Select(node => TextOf(syntax, node)));

    private static string TextOf(TomlSyntaxDocument syntax, TomlSyntaxNode node)
        => syntax.Source.Substring(node.Span.Start, node.Span.Length);

    private static string TextOf(TomlSyntaxDocument syntax, TomlSyntaxTrivia trivia)
        => syntax.Source.Substring(trivia.Span.Start, trivia.Span.Length);
}

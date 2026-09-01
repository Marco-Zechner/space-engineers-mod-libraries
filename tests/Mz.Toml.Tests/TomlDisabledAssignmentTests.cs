using System;
using System.Linq;
using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlDisabledAssignmentTests
{
    [Fact]
    public void Ordinary_Comments_Remain_Comments()
    {
        const string source =
            "# ordinary comment\n" +
            "#ordinary comment\n" +
            "value = 1\n";

        var result = Toml.TryParse(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(
        [
            TomlSyntaxNodeKind.Comment,
            TomlSyntaxNodeKind.Newline,
            TomlSyntaxNodeKind.Comment,
            TomlSyntaxNodeKind.Newline,
            TomlSyntaxNodeKind.Assignment,
            TomlSyntaxNodeKind.Newline
        ],
            result.Syntax.Nodes.Select(node => node.Kind)
        );
    }

    [Fact]
    public void Disabled_Assignment_Is_First_Class_Syntax_But_Not_Semantic_Data()
    {
        const string source =
            "#! value = 42\n" +
            "active = true\n";

        var result = Toml.TryParse(source);

        Assert.True(result.IsSuccess);
        Assert.False(result.Document.Root.ContainsKey("value"));
        Assert.True(result.Document.Root.ContainsKey("active"));

        Assert.Equal(TomlSyntaxNodeKind.DisabledAssignment, result.Syntax.Nodes[0].Kind);
        Assert.Equal("#! value = 42", TextOf(result.Syntax, result.Syntax.Nodes[0]));
        Assert.Equal(source, Reconstruct(result.Syntax));
    }

    [Fact]
    public void Disabled_Assignment_May_Duplicate_An_Active_Key()
    {
        const string source =
            "value = 1\n" +
            "#! value = 2\n";

        var result = Toml.TryParse(source);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Document.Root);
        Assert.True(result.Document.Root.ContainsKey("value"));
        Assert.Equal(
        [
            TomlSyntaxNodeKind.Assignment,
            TomlSyntaxNodeKind.Newline,
            TomlSyntaxNodeKind.DisabledAssignment,
            TomlSyntaxNodeKind.Newline
        ],
            result.Syntax.Nodes.Select(node => node.Kind)
        );
    }

    [Theory]
    [InlineData("#! scalar = -12\n")]
    [InlineData("#! dotted . key = \"value\"\n")]
    [InlineData("#! array = [1, 2, 3]\n")]
    [InlineData("#! inline = { enabled = true, count = 2 }\n")]
    [InlineData("#! temporal = 1979-05-27T07:32:00Z\n")]
    public void Disabled_Assignment_Reuses_Normal_Assignment_Value_Grammar(string source)
    {
        var result = Toml.TryParse(source);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Document.Root);
        Assert.Equal(TomlSyntaxNodeKind.DisabledAssignment, result.Syntax.Nodes[0].Kind);
        Assert.Equal(source.TrimEnd('\n'), TextOf(result.Syntax, result.Syntax.Nodes[0]));
        Assert.Equal(source, Reconstruct(result.Syntax));
    }

    [Fact]
    public void Disabled_Assignment_Is_Relative_To_Current_Table_But_Does_Not_Add_A_Value()
    {
        const string source =
            "[server]\n" +
            "#! port = 27016\n" +
            "name = \"Example\"\n";

        var result = Toml.TryParse(source);

        Assert.True(result.IsSuccess);

        var server = Assert.IsType<TomlTable>(result.Document.Root["server"]);

        Assert.False(server.ContainsKey("port"));
        Assert.True(server.ContainsKey("name"));
        Assert.Single(server);

        Assert.Contains(
            result.Syntax.Nodes,
            node => node.Kind == TomlSyntaxNodeKind.DisabledAssignment &&
                    TextOf(result.Syntax, node) == "#! port = 27016"
        );
    }

    [Fact]
    public void Disabled_Assignment_Preserves_Trailing_Trivia_And_Comment()
    {
        const string source =
            "  #! value = [1, 2]  # disabled\r\n" +
            "active = true\r\n";

        var result = Toml.TryParse(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(source, Reconstruct(result.Syntax));

        Assert.Equal(
        [
            TomlSyntaxNodeKind.Whitespace,
            TomlSyntaxNodeKind.DisabledAssignment,
            TomlSyntaxNodeKind.Whitespace,
            TomlSyntaxNodeKind.Comment,
            TomlSyntaxNodeKind.Newline,
            TomlSyntaxNodeKind.Assignment,
            TomlSyntaxNodeKind.Newline
        ],
            result.Syntax.Nodes.Select(node => node.Kind)
        );

        Assert.Equal("#! value = [1, 2]", TextOf(result.Syntax, result.Syntax.Nodes[1]));
        Assert.Equal("# disabled", TextOf(result.Syntax, result.Syntax.Nodes[3]));
    }

    [Fact]
    public void HashBang_In_Trailing_Comment_Position_Remains_A_Comment()
    {
        const string source = "value = 1 #! not a disabled assignment\n";

        var result = Toml.TryParse(source);

        Assert.True(result.IsSuccess);
        Assert.True(result.Document.Root.ContainsKey("value"));
        Assert.DoesNotContain(
            result.Syntax.Nodes,
            node => node.Kind == TomlSyntaxNodeKind.DisabledAssignment
        );
        Assert.Contains(
            result.Syntax.Nodes,
            node => node.Kind == TomlSyntaxNodeKind.Comment &&
                    TextOf(result.Syntax, node) == "#! not a disabled assignment"
        );
    }

    [Fact]
    public void HashBang_Inside_Array_Comment_Position_Remains_A_Comment()
    {
        const string source =
            "values = [\n" +
            "  1, #! still an array comment\n" +
            "  2\n" +
            "]\n";

        var result = Toml.TryParse(source);

        Assert.True(result.IsSuccess);
        Assert.True(result.Document.Root.ContainsKey("values"));
        Assert.DoesNotContain(
            result.Syntax.Nodes,
            node => node.Kind == TomlSyntaxNodeKind.DisabledAssignment
        );
        Assert.Contains(
            result.Syntax.Trivia,
            trivia => trivia.Kind == TomlSyntaxTriviaKind.Comment &&
                      TriviaTextOf(result.Syntax, trivia) == "#! still an array comment"
        );
    }

    [Fact]
    public void Disabled_Key_Does_Not_Block_Later_Active_Key_With_Same_Name()
    {
        const string source =
            "#! value = 2\n" +
            "value = 1\n";

        var result = Toml.TryParse(source);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Document.Root);
        Assert.True(result.Document.Root.ContainsKey("value"));

        Assert.Equal(
        [
            TomlSyntaxNodeKind.DisabledAssignment,
            TomlSyntaxNodeKind.Newline,
            TomlSyntaxNodeKind.Assignment,
            TomlSyntaxNodeKind.Newline
        ],
            result.Syntax.Nodes.Select(node => node.Kind)
        );
    }

    [Fact]
    public void Malformed_Disabled_Assignment_Preserves_Inner_Diagnostic_Position()
    {
        const string source = "  #! value =\n";

        var result = Toml.TryParse(source);

        Assert.False(result.IsSuccess);

        var diagnostic = Assert.Single(result.Diagnostics);

        Assert.Equal(TomlDiagnosticCode.InvalidDisabledAssignment, diagnostic.Code);
        Assert.Equal(1, diagnostic.Line);
        Assert.Equal(13, diagnostic.Column);
        Assert.Contains("Expected a value", diagnostic.Message, StringComparison.Ordinal);
    }
    [Theory]
    [InlineData("#!\n")]
    [InlineData("#! broken\n")]
    [InlineData("#! value =\n")]
    [InlineData("#! [server]\n")]
    public void Malformed_Disabled_Assignment_Produces_Extension_Diagnostic(string source)
    {
        var result = Toml.TryParse(source);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Document);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(TomlDiagnosticCode.InvalidDisabledAssignment, diagnostic.Code);
    }

    private static string Reconstruct(TomlSyntaxDocument syntax)
        => string.Concat(syntax.Nodes.Select(node => TextOf(syntax, node)));

    private static string TextOf(TomlSyntaxDocument syntax, TomlSyntaxNode node)
        => syntax.Source.Substring(node.Span.Start, node.Span.Length);

    private static string TriviaTextOf(TomlSyntaxDocument syntax, TomlSyntaxTrivia trivia)
        => syntax.Source.Substring(trivia.Span.Start, trivia.Span.Length);
}
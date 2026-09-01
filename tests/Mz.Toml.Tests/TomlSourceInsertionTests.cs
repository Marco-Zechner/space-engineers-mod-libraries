using System;
using System.Linq;
using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlSourceInsertionTests
{
    [Fact]
    public void InsertSourceBefore_Inserts_Exact_Fragment_Before_Owned_Node()
    {
        const string source =
            "first = 1\r\n" +
            "third = 3\r\n";

        var syntax = Toml.TryParse(source).Syntax;
        var third = syntax.Nodes.Single(node =>
            node.Kind == TomlSyntaxNodeKind.Assignment &&
            TextOf(syntax, node).StartsWith("third", StringComparison.Ordinal));

        var edited = syntax.InsertSourceBefore(third, "second = 2\r\n");

        Assert.Equal(
            "first = 1\r\n" +
            "second = 2\r\n" +
            "third = 3\r\n",
            edited
        );

        Assert.Equal(source, syntax.Source);

        var reparsed = Toml.TryParse(edited);

        Assert.True(reparsed.IsSuccess);
        Assert.Equal(["first", "second", "third"], reparsed.Document.Root.Keys);
    }

    [Fact]
    public void InsertSourceAfter_Newline_After_Header_Uses_Existing_Table_Context()
    {
        const string source =
            "[server]\n" +
            "name = \"Example\"\n";

        var syntax = Toml.TryParse(source).Syntax;
        var headerIndex = IndexOfKind(syntax, TomlSyntaxNodeKind.TableHeader);
        var newlineAfterHeader = syntax.Nodes[headerIndex + 1];

        Assert.Equal(TomlSyntaxNodeKind.Newline, newlineAfterHeader.Kind);

        var edited = syntax.InsertSourceAfter(newlineAfterHeader, "port = 27016\n");
        var reparsed = Toml.TryParse(edited);

        Assert.True(reparsed.IsSuccess);
        Assert.False(reparsed.Document.Root.ContainsKey("port"));

        var server = Assert.IsType<TomlTable>(reparsed.Document.Root["server"]);

        Assert.Equal(["port", "name"], server.Keys);
    }

    [Fact]
    public void InsertSourceBefore_Next_Header_Keeps_Assignment_In_Previous_Section()
    {
        const string source =
            "[server]\n" +
            "name = \"Example\"\n" +
            "\n" +
            "[client]\n" +
            "enabled = true\n";

        var syntax = Toml.TryParse(source).Syntax;
        var clientHeader = syntax.Nodes.Last(node => node.Kind == TomlSyntaxNodeKind.TableHeader);

        var edited = syntax.InsertSourceBefore(clientHeader, "port = 27016\n");
        var reparsed = Toml.TryParse(edited);

        Assert.True(reparsed.IsSuccess);

        var server = Assert.IsType<TomlTable>(reparsed.Document.Root["server"]);
        var client = Assert.IsType<TomlTable>(reparsed.Document.Root["client"]);

        Assert.True(server.ContainsKey("port"));
        Assert.False(client.ContainsKey("port"));
    }

    [Fact]
    public void InsertSourceBefore_Supports_Disabled_Assignment()
    {
        const string source =
            "active = true\n" +
            "after = 2\n";

        var syntax = Toml.TryParse(source).Syntax;
        var after = syntax.Nodes.Last(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var edited = syntax.InsertSourceBefore(after, "#! optional = [1, 2]\n");
        var reparsed = Toml.TryParse(edited);

        Assert.True(reparsed.IsSuccess);
        Assert.False(reparsed.Document.Root.ContainsKey("optional"));
        Assert.Contains(reparsed.Syntax.Nodes, node => node.Kind == TomlSyntaxNodeKind.DisabledAssignment);
    }

    [Fact]
    public void InsertSourceBefore_Supports_Exact_Comment_And_Blank_Line_Source()
    {
        const string source = "value = 1\r\n";

        var syntax = Toml.TryParse(source).Syntax;
        var assignment = syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var edited = syntax.InsertSourceBefore(assignment, "# generated description\r\n\r\n");

        Assert.Equal(
            "# generated description\r\n" +
            "\r\n" +
            "value = 1\r\n",
            edited
        );

        Assert.True(Toml.TryParse(edited).IsSuccess);
    }

    [Fact]
    public void InsertSourceAtStart_Supports_Empty_Document()
    {
        var syntax = Toml.TryParse(string.Empty).Syntax;

        var edited = syntax.InsertSourceAtStart("value = 1\n");
        var reparsed = Toml.TryParse(edited);

        Assert.Equal("value = 1\n", edited);
        Assert.True(reparsed.IsSuccess);
        Assert.True(reparsed.Document.Root.ContainsKey("value"));
        Assert.Equal(string.Empty, syntax.Source);
    }

    [Fact]
    public void InsertSourceAtEnd_Uses_Exact_Fragment_Without_Hidden_Newline_Policy()
    {
        const string source = "first = 1";

        var syntax = Toml.TryParse(source).Syntax;

        var edited = syntax.InsertSourceAtEnd("\nsecond = 2\r\n");

        Assert.Equal(
            "first = 1\n" +
            "second = 2\r\n",
            edited
        );

        var reparsed = Toml.TryParse(edited);

        Assert.True(reparsed.IsSuccess);
        Assert.Equal(["first", "second"], reparsed.Document.Root.Keys);
    }

    [Fact]
    public void InsertSourceBefore_Preserves_Unrelated_Source_Exactly()
    {
        const string source =
            "# heading\r\n" +
            "first\t =  [ 1, 2 ]  # player comment\r\n" +
            "third = \"keep\"\r\n";

        var syntax = Toml.TryParse(source).Syntax;
        var third = syntax.Nodes.Single(node =>
            node.Kind == TomlSyntaxNodeKind.Assignment &&
            TextOf(syntax, node).StartsWith("third", StringComparison.Ordinal));

        var edited = syntax.InsertSourceBefore(third, "second = 2\r\n");

        Assert.Equal(
            "# heading\r\n" +
            "first\t =  [ 1, 2 ]  # player comment\r\n" +
            "second = 2\r\n" +
            "third = \"keep\"\r\n",
            edited
        );

        Assert.Equal(source, syntax.Source);
    }

    [Theory]
    [InlineData("")]
    [InlineData("broken")]
    [InlineData("value =")]
    [InlineData("[broken")]
    public void InsertSource_Rejects_Fragment_That_Is_Not_Valid_Standalone_Toml(string sourceFragment)
    {
        var syntax = Toml.TryParse("value = 1\n").Syntax;
        var assignment = syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var exception = Assert.Throws<ArgumentException>(() => syntax.InsertSourceBefore(assignment, sourceFragment));

        Assert.Equal("sourceFragment", exception.ParamName);
        Assert.Equal("value = 1\n", syntax.Source);
    }

    [Fact]
    public void InsertSource_Rejects_Fragment_When_Resulting_Document_Would_Be_Invalid()
    {
        const string source =
            "value = 1\n" +
            "after = 2\n";

        var syntax = Toml.TryParse(source).Syntax;
        var after = syntax.Nodes.Last(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var exception = Assert.Throws<ArgumentException>(() => syntax.InsertSourceBefore(after, "value = 3\n"));

        Assert.Equal("sourceFragment", exception.ParamName);
        Assert.Equal(source, syntax.Source);
    }

    [Fact]
    public void InsertSourceAfter_Rejects_Fragment_That_Would_Fuse_With_Anchor()
    {
        const string source = "value = 1\n";

        var syntax = Toml.TryParse(source).Syntax;
        var assignment = syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var exception = Assert.Throws<ArgumentException>(() => syntax.InsertSourceAfter(assignment, "other = 2"));

        Assert.Equal("sourceFragment", exception.ParamName);
    }

    [Fact]
    public void InsertSourceBefore_Rejects_Null_Fragment()
    {
        var syntax = Toml.TryParse("value = 1\n").Syntax;
        var assignment = syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var exception = Assert.Throws<ArgumentNullException>(() => syntax.InsertSourceBefore(assignment, null));

        Assert.Equal("sourceFragment", exception.ParamName);
    }

    [Fact]
    public void InsertSourceAtEnd_Rejects_Null_Fragment()
    {
        var syntax = Toml.TryParse("value = 1\n").Syntax;

        var exception = Assert.Throws<ArgumentNullException>(() => syntax.InsertSourceAtEnd(null));

        Assert.Equal("sourceFragment", exception.ParamName);
    }

    [Fact]
    public void InsertSourceBefore_Rejects_Node_From_Another_Syntax_Document()
    {
        var first = Toml.TryParse("value = 1\n").Syntax;
        var second = Toml.TryParse("value = 1\n").Syntax;
        var foreign = second.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var exception = Assert.Throws<ArgumentException>(() => first.InsertSourceBefore(foreign, "other = 2\n"));

        Assert.Equal("node", exception.ParamName);
    }

    [Fact]
    public void InsertSourceAfter_Rejects_Node_From_Another_Syntax_Document()
    {
        var first = Toml.TryParse("value = 1\n").Syntax;
        var second = Toml.TryParse("value = 1\n").Syntax;
        var foreign = second.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var exception = Assert.Throws<ArgumentException>(() => first.InsertSourceAfter(foreign, "\nother = 2"));

        Assert.Equal("node", exception.ParamName);
    }

    private static int IndexOfKind(TomlSyntaxDocument syntax, TomlSyntaxNodeKind kind)
    {
        for (var index = 0; index < syntax.Nodes.Count; index++)
            if (syntax.Nodes[index].Kind == kind)
                return index;
        
        throw new InvalidOperationException("Expected syntax node kind was not found.");
    }

    private static string TextOf(TomlSyntaxDocument syntax, TomlSyntaxNode node)
        => syntax.Source.Substring(node.Span.Start, node.Span.Length);
}

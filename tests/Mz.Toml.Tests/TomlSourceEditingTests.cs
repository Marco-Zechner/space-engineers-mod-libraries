using System;
using System.Linq;
using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlSourceEditingTests
{
    [Fact]
    public void DisableAssignment_Inserts_Only_HashBang_At_Assignment_Start()
    {
        const string source =
            "# heading\r\n" +
            "  value = 42  # keep this\r\n" +
            "next = true\r\n";

        var syntax = Toml.TryParse(source).Syntax;
        var assignment = syntax.Nodes.Single(node =>
            node.Kind == TomlSyntaxNodeKind.Assignment &&
            TextOf(syntax, node).StartsWith("value", StringComparison.Ordinal));

        var edited = syntax.DisableAssignment(assignment);

        Assert.Equal(
            "# heading\r\n" +
            "  #!value = 42  # keep this\r\n" +
            "next = true\r\n",
            edited
        );

        Assert.Equal(source, syntax.Source);
        Assert.Equal("value = 42", TextOf(syntax, assignment));
    }

    [Fact]
    public void EnableAssignment_Removes_Only_HashBang_Marker()
    {
        const string source =
            "# heading\n" +
            "#! value = 42  # keep this\n" +
            "next = true\n";

        var syntax = Toml.TryParse(source).Syntax;
        var disabled = syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.DisabledAssignment);

        var edited = syntax.EnableAssignment(disabled);

        Assert.Equal(
            "# heading\n" +
            " value = 42  # keep this\n" +
            "next = true\n",
            edited
        );

        Assert.Equal(source, syntax.Source);
        Assert.Equal("#! value = 42", TextOf(syntax, disabled));
    }

    [Fact]
    public void Disable_Then_Enable_Restores_Exact_Original_Assignment_Source()
    {
        const string source = "value\t =  [ 1, 2 ] # note\n";

        var original = Toml.TryParse(source).Syntax;
        var active = original.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var disabledSource = original.DisableAssignment(active);
        var disabled = Toml.TryParse(disabledSource).Syntax;
        var disabledNode = disabled.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.DisabledAssignment);

        var restored = disabled.EnableAssignment(disabledNode);

        Assert.Equal(source, restored);
    }

    [Fact]
    public void DisableAssignment_Preserves_Multiline_Array_Layout_Exactly()
    {
        const string source =
            "values = [\r\n" +
            "  1, # first\r\n" +
            "  # between\r\n" +
            "  2,\r\n" +
            "]  # trailing\r\n" +
            "after = true\r\n";

        var syntax = Toml.TryParse(source).Syntax;
        var assignment = syntax.Nodes.Single(node =>
            node.Kind == TomlSyntaxNodeKind.Assignment &&
            TextOf(syntax, node).StartsWith("values", StringComparison.Ordinal));

        var edited = syntax.DisableAssignment(assignment);

        Assert.Equal("#!" + source, edited);
    }

    [Fact]
    public void DisableAssignment_Reparse_Removes_Value_From_Semantic_Document()
    {
        const string source =
            "value = 42\n" +
            "other = true\n";

        var original = Toml.TryParse(source);
        var assignment = original.Syntax.Nodes.First(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var edited = original.Syntax.DisableAssignment(assignment);
        var reparsed = Toml.TryParse(edited);

        Assert.True(reparsed.IsSuccess);
        Assert.False(reparsed.Document.Root.ContainsKey("value"));
        Assert.True(reparsed.Document.Root.ContainsKey("other"));
        Assert.Contains(
            reparsed.Syntax.Nodes,
            node => node.Kind == TomlSyntaxNodeKind.DisabledAssignment
        );
    }

    [Fact]
    public void EnableAssignment_Reparse_Adds_Value_To_Semantic_Document()
    {
        const string source =
            "#!value = 42\n" +
            "other = true\n";

        var original = Toml.TryParse(source);
        var disabled = original.Syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.DisabledAssignment);

        var edited = original.Syntax.EnableAssignment(disabled);
        var reparsed = Toml.TryParse(edited);

        Assert.True(reparsed.IsSuccess);
        Assert.True(reparsed.Document.Root.ContainsKey("value"));
        Assert.True(reparsed.Document.Root.ContainsKey("other"));
        Assert.DoesNotContain(
            reparsed.Syntax.Nodes,
            node => node.Kind == TomlSyntaxNodeKind.DisabledAssignment
        );
    }

    [Fact]
    public void DisableAssignment_Rejects_Null_Node()
    {
        var syntax = Toml.TryParse("value = 1\n").Syntax;

        var exception = Assert.Throws<ArgumentNullException>(() => syntax.DisableAssignment(null));

        Assert.Equal("node", exception.ParamName);
    }

    [Fact]
    public void EnableAssignment_Rejects_Null_Node()
    {
        var syntax = Toml.TryParse("#!value = 1\n").Syntax;

        var exception = Assert.Throws<ArgumentNullException>(() => syntax.EnableAssignment(null));

        Assert.Equal("node", exception.ParamName);
    }

    [Fact]
    public void DisableAssignment_Rejects_Wrong_Syntax_Kind()
    {
        var syntax = Toml.TryParse("# comment\nvalue = 1\n").Syntax;
        var comment = syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Comment);

        var exception = Assert.Throws<ArgumentException>(() => syntax.DisableAssignment(comment));

        Assert.Equal("node", exception.ParamName);
    }

    [Fact]
    public void EnableAssignment_Rejects_Wrong_Syntax_Kind()
    {
        var syntax = Toml.TryParse("value = 1\n").Syntax;
        var active = syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var exception = Assert.Throws<ArgumentException>(() => syntax.EnableAssignment(active));

        Assert.Equal("node", exception.ParamName);
    }

    [Fact]
    public void DisableAssignment_Rejects_Node_From_Another_Syntax_Document()
    {
        var first = Toml.TryParse("value = 1\n").Syntax;
        var second = Toml.TryParse("value = 1\n").Syntax;
        var foreignNode = second.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var exception = Assert.Throws<ArgumentException>(() => first.DisableAssignment(foreignNode));

        Assert.Equal("node", exception.ParamName);
    }

    [Fact]
    public void EnableAssignment_Rejects_Node_From_Another_Syntax_Document()
    {
        var first = Toml.TryParse("#!value = 1\n").Syntax;
        var second = Toml.TryParse("#!value = 1\n").Syntax;
        var foreignNode = second.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.DisabledAssignment);

        var exception = Assert.Throws<ArgumentException>(() => first.EnableAssignment(foreignNode));

        Assert.Equal("node", exception.ParamName);
    }

    private static string TextOf(TomlSyntaxDocument syntax, TomlSyntaxNode node)
        => syntax.Source.Substring(node.Span.Start, node.Span.Length);
}

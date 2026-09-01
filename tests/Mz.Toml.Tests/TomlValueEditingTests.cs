using System;
using System.Linq;
using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlValueEditingTests
{
    [Fact]
    public void Assignment_ValueSpan_Identifies_Only_The_Value_Source()
    {
        const string source = "name . child =   \"hello world\"  # keep\n";

        var syntax = Toml.TryParse(source).Syntax;
        var assignment = syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        Assert.True(assignment.ValueSpan.HasValue);
        Assert.Equal("\"hello world\"", TextOf(syntax, assignment.ValueSpan.Value));
    }

    [Fact]
    public void DisabledAssignment_ValueSpan_Identifies_Only_The_Value_Source()
    {
        const string source =
            "#! values = [\r\n" +
            "  1, # first\r\n" +
            "  2,\r\n" +
            "]  # keep\r\n";

        var syntax = Toml.TryParse(source).Syntax;
        var disabled = syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.DisabledAssignment);

        Assert.True(disabled.ValueSpan.HasValue);
        Assert.Equal(
            "[\r\n" +
            "  1, # first\r\n" +
            "  2,\r\n" +
            "]",
            TextOf(syntax, disabled.ValueSpan.Value)
        );
    }

    [Theory]
    [InlineData("# comment\n")]
    [InlineData("[table]\n")]
    [InlineData("\n")]
    public void NonAssignment_Nodes_Do_Not_Have_ValueSpan(string source)
    {
        var syntax = Toml.TryParse(source).Syntax;

        Assert.All(syntax.Nodes, node => Assert.False(node.ValueSpan.HasValue));
    }

    [Fact]
    public void ReplaceAssignmentValue_Replaces_Only_The_Exact_Value_Span()
    {
        const string source =
            "# heading\r\n" +
            "  name . child =   \"old\"  # keep this\r\n" +
            "next = true\r\n";

        var syntax = Toml.TryParse(source).Syntax;
        var assignment = syntax.Nodes.Single(node =>
            node.Kind == TomlSyntaxNodeKind.Assignment &&
            TextOf(syntax, node).Contains("name . child", StringComparison.Ordinal));

        var edited = syntax.ReplaceAssignmentValue(assignment, "\"new\"");

        Assert.Equal(
            "# heading\r\n" +
            "  name . child =   \"new\"  # keep this\r\n" +
            "next = true\r\n",
            edited
        );

        Assert.Equal(source, syntax.Source);
        Assert.True(assignment.ValueSpan.HasValue);
        Assert.Equal("\"old\"", TextOf(syntax, assignment.ValueSpan.GetValueOrDefault()));
    }

    [Fact]
    public void ReplaceAssignmentValue_Preserves_Disabled_Marker_And_Trailing_Comment()
    {
        const string source = "  #! value = 1_000  # disabled\n";

        var syntax = Toml.TryParse(source).Syntax;
        var disabled = syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.DisabledAssignment);

        var edited = syntax.ReplaceAssignmentValue(disabled, "0x2A");

        Assert.Equal("  #! value = 0x2A  # disabled\n", edited);

        var reparsed = Toml.TryParse(edited);

        Assert.True(reparsed.IsSuccess);
        Assert.False(reparsed.Document.Root.ContainsKey("value"));
        Assert.Equal(
            TomlSyntaxNodeKind.DisabledAssignment,
            reparsed.Syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.DisabledAssignment).Kind
        );
    }

    [Fact]
    public void ReplaceAssignmentValue_Accepts_Multiline_Array_Value()
    {
        const string source =
            "values = [1, 2]  # trailing\r\n" +
            "after = true\r\n";

        const string replacement =
            "[\r\n" +
            "  3,\r\n" +
            "  # generated later\r\n" +
            "  4,\r\n" +
            "]";

        var syntax = Toml.TryParse(source).Syntax;
        var assignment = syntax.Nodes.First(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var edited = syntax.ReplaceAssignmentValue(assignment, replacement);

        Assert.Equal(
            "values = " + replacement + "  # trailing\r\n" +
            "after = true\r\n",
            edited
        );

        var reparsed = Toml.TryParse(edited);

        Assert.True(reparsed.IsSuccess);
        var array = Assert.IsType<TomlArray>(reparsed.Document.Root["values"]);
        Assert.Equal(2, array.Count);
    }

    [Fact]
    public void ReplaceAssignmentValue_Reparse_Uses_New_Active_Value()
    {
        const string source =
            "value = 42\n" +
            "other = true\n";

        var parsed = Toml.TryParse(source);
        var assignment = parsed.Syntax.Nodes.First(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var edited = parsed.Syntax.ReplaceAssignmentValue(assignment, "\"replacement\"");
        var reparsed = Toml.TryParse(edited);

        Assert.True(reparsed.IsSuccess);

        var value = Assert.IsType<TomlValue>(reparsed.Document.Root["value"]);

        Assert.Equal(TomlValueKind.String, value.ValueKind);
        Assert.Equal("replacement", value.AsString());
        Assert.True(reparsed.Document.Root.ContainsKey("other"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" 1")]
    [InlineData("1 ")]
    [InlineData("1 # comment")]
    [InlineData("1\n")]
    [InlineData("1\r\n")]
    [InlineData("broken")]
    [InlineData("[1,")]
    public void ReplaceAssignmentValue_Rejects_Text_That_Is_Not_Exactly_One_Toml_Value(string replacement)
    {
        var syntax = Toml.TryParse("value = 1\n").Syntax;
        var assignment = syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var exception = Assert.Throws<ArgumentException>(() => syntax.ReplaceAssignmentValue(assignment, replacement));

        Assert.Equal("valueSource", exception.ParamName);
        Assert.Equal("value = 1\n", syntax.Source);
    }

    [Fact]
    public void ReplaceAssignmentValue_Rejects_Null_Value_Source()
    {
        var syntax = Toml.TryParse("value = 1\n").Syntax;
        var assignment = syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var exception = Assert.Throws<ArgumentNullException>(() => syntax.ReplaceAssignmentValue(assignment, null));

        Assert.Equal("valueSource", exception.ParamName);
    }

    [Fact]
    public void ReplaceAssignmentValue_Rejects_Wrong_Syntax_Kind()
    {
        var syntax = Toml.TryParse("# comment\nvalue = 1\n").Syntax;
        var comment = syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Comment);

        var exception = Assert.Throws<ArgumentException>(() => syntax.ReplaceAssignmentValue(comment, "2"));

        Assert.Equal("node", exception.ParamName);
    }

    [Fact]
    public void ReplaceAssignmentValue_Rejects_Node_From_Another_Syntax_Document()
    {
        var first = Toml.TryParse("value = 1\n").Syntax;
        var second = Toml.TryParse("value = 1\n").Syntax;
        var foreign = second.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var exception = Assert.Throws<ArgumentException>(() => first.ReplaceAssignmentValue(foreign, "2"));

        Assert.Equal("node", exception.ParamName);
    }

    private static string TextOf(TomlSyntaxDocument syntax, TomlSyntaxNode node)
        => TextOf(syntax, node.Span);

    private static string TextOf(TomlSyntaxDocument syntax, TomlSourceSpan span)
        => syntax.Source.Substring(span.Start, span.Length);
}

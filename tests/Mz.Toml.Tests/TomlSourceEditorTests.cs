using System;
using System.Linq;
using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlSourceEditorTests
{
    [Fact]
    public void CreateEditor_Preserves_Exact_Source_And_Initial_Syntax_Instance()
    {
        const string source =
            "# heading\r\n" +
            "value = 1\r\n";

        var syntax = Toml.TryParse(source).Syntax;
        var editor = syntax.CreateEditor();

        Assert.Equal(source, editor.Source);
        Assert.Same(syntax, editor.Syntax);
    }

    [Fact]
    public void Editor_Can_Compose_Disable_And_Value_Replacement()
    {
        const string source =
            "# heading\r\n" +
            "value = 1  # keep\r\n" +
            "other = true\r\n";

        var syntax = Toml.TryParse(source).Syntax;
        var active = syntax.Nodes.Single(node =>
            node.Kind == TomlSyntaxNodeKind.Assignment &&
            TextOf(syntax, node).StartsWith("value", StringComparison.Ordinal)
        );

        var editor = syntax.CreateEditor();

        editor.DisableAssignment(active);

        var disabled = editor.Syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.DisabledAssignment);

        editor.ReplaceAssignmentValue(disabled, "[2, 3]");

        Assert.Equal(
            "# heading\r\n" +
            "#!value = [2, 3]  # keep\r\n" +
            "other = true\r\n",
            editor.Source
        );

        var reparsed = Toml.TryParse(editor.Source);

        Assert.True(reparsed.IsSuccess);
        Assert.False(reparsed.Document.Root.ContainsKey("value"));
        Assert.True(reparsed.Document.Root.ContainsKey("other"));
    }

    [Fact]
    public void Editor_Refreshes_Syntax_After_Each_Edit()
    {
        var syntax = Toml.TryParse("value = 1\n").Syntax;
        var originalSyntax = syntax;
        var originalNode = syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var editor = syntax.CreateEditor();

        editor.ReplaceAssignmentValue(originalNode, "2");

        Assert.NotSame(originalSyntax, editor.Syntax);
        Assert.Equal("value = 2\n", editor.Source);

        var refreshed = editor.Syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        Assert.NotSame(originalNode, refreshed);
        Assert.Equal("2", TextOf(editor.Syntax, refreshed.ValueSpan.GetValueOrDefault()));
    }

    [Fact]
    public void Editor_Rejects_Node_From_Previous_Editor_State()
    {
        var syntax = Toml.TryParse(
            "first = 1\n" +
            "second = 2\n"
        ).Syntax;

        var first = syntax.Nodes.First(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var second = syntax.Nodes.Last(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        var editor = syntax.CreateEditor();

        editor.ReplaceAssignmentValue(first, "10");

        var exception = Assert.Throws<ArgumentException>(() => editor.ReplaceAssignmentValue(second, "20"));

        Assert.Equal("node", exception.ParamName);
        Assert.Equal(
            "first = 10\n" +
            "second = 2\n",
            editor.Source
        );
    }

    [Fact]
    public void Editor_Composes_Insertion_With_Refreshed_Assignment_Edit()
    {
        const string source =
            "# heading\n" +
            "value = 1\n";

        var syntax = Toml.TryParse(source).Syntax;
        var editor = syntax.CreateEditor();

        editor.InsertSourceAtStart("# generated\n");

        var assignment = editor.Syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        editor.ReplaceAssignmentValue(assignment, "42");

        Assert.Equal(
            "# generated\n" +
            "# heading\n" +
            "value = 42\n",
            editor.Source
        );
    }

    [Fact]
    public void Editor_Enable_That_Would_Create_Duplicate_Key_Is_Atomic()
    {
        const string source =
            "value = 1\n" +
            "#!value = 2\n";

        var syntax = Toml.TryParse(source).Syntax;
        var editor = syntax.CreateEditor();
        var beforeSyntax = editor.Syntax;

        var disabled = editor.Syntax.Nodes.Single(node => node.Kind == TomlSyntaxNodeKind.DisabledAssignment);

        Assert.Throws<InvalidOperationException>(() => editor.EnableAssignment(disabled));

        Assert.Equal(source, editor.Source);
        Assert.Same(beforeSyntax, editor.Syntax);

        var reparsed = Toml.TryParse(editor.Source);

        Assert.True(reparsed.IsSuccess);
        Assert.Equal(1L, reparsed.Document.Root.AsValue("value").AsInteger());
    }

    [Fact]
    public void CreateEditor_Rejects_Syntax_With_Unparsed_Source()
    {
        var failed = Toml.TryParse(
            "first = 1\n" +
            "broken = [1,\n"
        );

        Assert.False(failed.IsSuccess);
        Assert.NotNull(failed.Syntax);
        Assert.Contains(failed.Syntax.Nodes, node => node.Kind == TomlSyntaxNodeKind.Unparsed);

        Assert.Throws<InvalidOperationException>(() => failed.Syntax.CreateEditor());
    }

    [Fact]
    public void CreateEditor_Rejects_Fully_Classified_Semantic_Failure()
    {
        var failed = Toml.TryParse(
            "value = 1\n" +
            "value = 2\n"
        );

        Assert.False(failed.IsSuccess);
        Assert.NotNull(failed.Syntax);
        Assert.DoesNotContain(failed.Syntax.Nodes, node => node.Kind == TomlSyntaxNodeKind.Unparsed);

        Assert.Throws<InvalidOperationException>(() => failed.Syntax.CreateEditor());
    }

    [Fact]
    public void Editor_Preserves_Exact_Unrelated_Formatting_Across_Multiple_Edits()
    {
        const string source =
            "# user heading\r\n" +
            "first\t =  [ 1, 2 ]  # player comment\r\n" +
            "second = \"old\"\r\n";

        var syntax = Toml.TryParse(source).Syntax;
        var editor = syntax.CreateEditor();

        var second = editor.Syntax.Nodes.Last(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        editor.ReplaceAssignmentValue(second, "\"new\"");

        var first = editor.Syntax.Nodes.First(node => node.Kind == TomlSyntaxNodeKind.Assignment);

        editor.DisableAssignment(first);

        Assert.Equal(
            "# user heading\r\n" +
            "#!first\t =  [ 1, 2 ]  # player comment\r\n" +
            "second = \"new\"\r\n",
            editor.Source
        );
    }

    private static string TextOf(TomlSyntaxDocument syntax, TomlSyntaxNode node)
        => TextOf(syntax, node.Span);

    private static string TextOf(TomlSyntaxDocument syntax, TomlSourceSpan span)
        => syntax.Source.Substring(span.Start, span.Length);
}

using Mz.Toml;
using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlKeyAndTableTests
{
    [Fact]
    public void Parses_Dotted_And_Mixed_Quoted_Keys()
    {
        var document = Toml.Parse("""
            name.first = "Arthur"
            "name".'last' = "Dent"
            count.a = 1
            count . b = 2
            "count" . "c" = 3
            'count'.'d' = 4
            many.dots.dot.dot.dot = 42

            """);

        var name = document.Root.AsTable("name");

        Assert.Equal("Arthur", name.AsValue("first").AsString());
        Assert.Equal("Dent", name.AsValue("last").AsString());

        var count = document.Root.AsTable("count");

        Assert.Equal(1L, count.AsValue("a").AsInteger());
        Assert.Equal(2L, count.AsValue("b").AsInteger());
        Assert.Equal(3L, count.AsValue("c").AsInteger());
        Assert.Equal(4L, count.AsValue("d").AsInteger());

        var many = document.Root.AsTable("many");
        var dots = many.AsTable("dots");
        var dot1 = dots.AsTable("dot");
        var dot2 = dot1.AsTable("dot");

        Assert.Equal(42L, dot2.AsValue("dot").AsInteger());
    }

    [Fact]
    public void Quoted_Dots_Stay_Inside_One_Key_Segment()
    {
        var document = Toml.Parse("""
            "with.dot" = 2
            [table.withdot]
            "key.with.dots" = 6
            "escaped\u002edot" = 7

            """);

        Assert.Equal(2L, document.Root.AsValue("with.dot").AsInteger());
        var table = document.Root.AsTable("table").AsTable("withdot");
        Assert.Equal(6L, table.AsValue("key.with.dots").AsInteger());
        Assert.Equal(7L, table.AsValue("escaped.dot").AsInteger());
    }

    [Theory]
    [InlineData("\"\" = \"blank\"\n")]
    [InlineData("'' = \"blank\"\n")]
    public void Empty_Quoted_Root_Key_Can_Be_A_Value(string text)
    {
        var document = Toml.Parse(text);

        Assert.Equal("blank", document.Root.AsValue("").AsString());
    }

    [Fact]
    public void Empty_Quoted_Key_Segments_Are_Valid()
    {
        var document = Toml.Parse("""
            ''.x = "empty.x"
            x."" = "x.empty"
            [a]
            "".'' = "empty.empty"

            """);

        Assert.Equal("empty.x", document.Root.AsTable("").AsValue("x").AsString());
        Assert.Equal("x.empty", document.Root.AsTable("x").AsValue("").AsString());
        Assert.Equal("empty.empty", document.Root.AsTable("a").AsTable("").AsValue("").AsString());
    }

    [Fact]
    public void Basic_And_Literal_Quoted_Keys_Have_Different_Escape_Rules()
    {
        var document = Toml.Parse("""
            "\u0061" = 1
            '\u0061' = 2

            """);

        Assert.Equal(1L, document.Root.AsValue("a").AsInteger());
        Assert.Equal(2L, document.Root.AsValue("\\u0061").AsInteger());
    }

    [Fact]
    public void Parses_Nested_Explicit_Tables()
    {
        var document = Toml.Parse("""
            [a]
            key = 1
            [a.extend]
            key = 2
            [a.extend.more]
            key = 3

            """);

        var a = document.Root.AsTable("a");
        Assert.Equal(1L, a.AsValue("key").AsInteger());
        var extend = a.AsTable("extend");
        Assert.Equal(2L, extend.AsValue("key").AsInteger());
        Assert.Equal(3L, extend.AsTable("more").AsValue("key").AsInteger());
    }

    [Fact]
    public void Omitted_Super_Tables_Are_Created_Implicitly()
    {
        var document = Toml.Parse("""
            [x.y.z.w]
            a = 1
            [x]
            b = 2

            """);

        var x = document.Root.AsTable("x");
        Assert.Equal(2L, x.AsValue("b").AsInteger());
        var w = x.AsTable("y").AsTable("z").AsTable("w");
        Assert.Equal(1L, w.AsValue("a").AsInteger());
    }

    [Fact]
    public void Explicit_Parent_Can_Appear_Before_Child()
    {
        var document = Toml.Parse("""
            [a]
            better = 43
            [a.b.c]
            answer = 42

            """);

        Assert.Equal(43L, document.Root.AsTable("a").AsValue("better").AsInteger());
        Assert.Equal(42L, document.Root.AsTable("a").AsTable("b").AsTable("c").AsValue("answer").AsInteger());
    }

    [Fact]
    public void New_Subtable_Can_Be_Declared_Under_Dotted_Key_Table()
    {
        var document = Toml.Parse("""
            a.b.value = 1
            [a.b.extra]
            value = 2

            """);

        var b = document.Root.AsTable("a").AsTable("b");

        Assert.Equal(1L, b.AsValue("value").AsInteger());
        Assert.Equal(2L, b.AsTable("extra").AsValue("value").AsInteger());
    }

    [Fact]
    public void Writer_Emits_Canonical_Nested_Tables()
    {
        var document = new TomlDocument();

        document.Root.Set("root", TomlValue.FromInteger(1));

        var a = new TomlTable();
        a.Set("with.dot", TomlValue.FromInteger(2));

        var b = new TomlTable();
        b.Set("answer", TomlValue.FromInteger(42));

        a.Set("b", b);
        document.Root.Set("a", a);

        var empty = new TomlTable();
        document.Root.Set("", empty);

        Assert.Equal("""
             root = 1

             [a]
             "with.dot" = 2

             [a.b]
             answer = 42

             [""]

             """.ReplaceLineEndings("\n"),
            Toml.Write(document));
    }

    [Fact]
    public void Parsed_Dotted_Keys_Can_Roundtrip_As_Canonical_Tables()
    {
        var first = Toml.Parse("""
            name.first = "Arthur"
            name.last = "Dent"

            """);

        var text = Toml.Write(first);

        Assert.Equal("""
            [name]
            first = "Arthur"
            last = "Dent"

            """.ReplaceLineEndings("\n"),
            text);

        var second = Toml.Parse(text);
        var name = second.Root.AsTable("name");

        Assert.Equal("Arthur", name.AsValue("first").AsString());
        Assert.Equal("Dent", name.AsValue("last").AsString());
    }
}

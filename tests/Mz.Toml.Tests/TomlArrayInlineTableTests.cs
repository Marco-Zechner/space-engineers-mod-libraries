using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlArrayInlineTableTests
{
    [Fact]
    public void Parses_Heterogeneous_Array()
    {
        var document = Toml.Parse("mixed = [1, 1.5, \"two\", true, [3], { four = 4 }]\n");

        var array = Assert.IsType<TomlArray>(document.Root["mixed"]);

        Assert.Equal(6, array.Count);
        Assert.Equal(1L, array.AsValue(0).AsInteger());
        Assert.Equal(1.5, array.AsValue(1).AsFloat());
        Assert.Equal("two", array.AsValue(2).AsString());
        Assert.True(array.AsValue(3).AsBoolean());

        var innerArray1 = array.AsArray(4);
        Assert.Equal(3L, innerArray1.AsValue(0).AsInteger());

        var innerArray2 = array.AsTable(5);
        Assert.Equal(4L, innerArray2.AsValue("four").AsInteger());
    }

    [Fact]
    public void Array_Allows_Comments_Newlines_And_Trailing_Comma()
    {
        var document = Toml.Parse("""
            values = [
            1,
            2, # comment
            3,
            ]

            """);

        var array = document.Root.AsArray("values");

        Assert.Equal(3, array.Count);
    }

    [Fact]
    public void Array_Can_Nest_Arbitrarily()
    {
        var document = Toml.Parse("value = [[[[[]]]]]\n");

        var current = document.Root.AsArray("value");

        for (var i = 0; i < 4; i++)
        {
            Assert.Single(current);
            current = current.AsArray(0);
        }
        Assert.Empty(current);
    }

    [Theory]
    [InlineData("a = [,]\n")]
    [InlineData("a = [,,]\n")]
    [InlineData("a = [1,,2]\n")]
    [InlineData("a = [1 2]\n")]
    [InlineData("a = [true false]\n")]
    [InlineData("a = [1,\n")]
    [InlineData("a = [\n")]
    public void Invalid_Array_Separators_And_Closures_Are_Rejected(string text)
        => Assert.False(Toml.TryParse(text).IsSuccess);

    [Fact]
    public void Parses_Inline_Table()
    {
        var document = Toml.Parse("point = { x = 1, y = 2 }\n");

        var point = document.Root.AsTable("point");

        Assert.Equal(1L, point.AsValue("x").AsInteger());
        Assert.Equal(2L, point.AsValue("y").AsInteger());
    }

    [Fact]
    public void Inline_Table_Allows_Dotted_Keys()
    {
        var document = Toml.Parse("value = { a.b.c = 1, a.b.d = 2 }\n");

        var value = document.Root.AsTable("value");

        var a = value.AsTable("a");
        var b = a.AsTable("b");

        Assert.Equal(1L, b.AsValue("c").AsInteger());
        Assert.Equal(2L, b.AsValue("d").AsInteger());
    }

    [Fact]
    public void Inline_Tables_And_Arrays_Can_Nest()
    {
        var document = Toml.Parse("value = [{ a = { b = [1, { c = 2 }] } }]\n");

        var rootArray = document.Root.AsArray("value");

        var outer = rootArray.AsTable(0);
        var nested = outer.AsTable("a");
        var values = nested.AsArray("b");
        var inner = values.AsTable(1);
        var c = inner.AsValue("c");

        Assert.Equal(2L, c.AsInteger());
    }

    [Fact]
    public void Inline_Table_Allows_Newline_Inside_Array_Value()
    {
        var document = Toml.Parse("""
            value = { a = [
              1,
              2,
            ] }
  
            """);

        var table = document.Root.AsTable("value");

        Assert.Equal(2, table.AsArray("a").Count);
    }

    [Fact]
    public void Inline_Table_Allows_Newline_Inside_Multiline_String()
    {
        var document = Toml.Parse(""""
            value = { a = """
            line
            """, b = 2 }

            """");

        var table = document.Root.AsTable("value");

        Assert.Equal("line\n", table.AsValue("a").AsString());
    }

    [Theory]
    [InlineData("a = { x = 1, }\n")]
    [InlineData("a = {,}\n")]
    [InlineData("a = {x = 1,, y = 2}\n")]
    [InlineData("a = {x = 1 y = 2}\n")]
    [InlineData("a = {x = 1,\ny = 2}\n")]
    [InlineData("a = {x = 1\n}\n")]
    [InlineData("a = {\nx = 1}\n")]
    public void Toml_10_Inline_Table_Layout_Rules_Are_Enforced(string text)
        => Assert.False(Toml.TryParse(text).IsSuccess);

    [Fact]
    public void Duplicate_Inline_Table_Key_Is_Rejected()
        => Assert.False(Toml.TryParse("a = { b = 1, b = 2 }\n").IsSuccess);

    [Fact]
    public void Inline_Table_Cannot_Extend_Nested_Inline_Table()
        => Assert.False(Toml.TryParse("a = { b = { c = 1 }, b.d = 2 }\n").IsSuccess);

    [Fact]
    public void Completed_Inline_Table_Cannot_Be_Extended_By_Dotted_Key()
        => Assert.False(Toml.TryParse("""
            a = { b = 1 }
            a.c = 2

            """).IsSuccess);

    [Fact]
    public void Completed_Inline_Table_Cannot_Be_Extended_By_Header()
        => Assert.False(Toml.TryParse("""
            a = {}
            [a.b]
            c = 1

            """).IsSuccess);

    [Fact]
    public void Array_Of_Inline_Tables_Is_Not_Array_Of_Tables_Syntax()
    {
        var document = Toml.Parse("items = [{x = 1}, {x = 2}]\n");

        var array = document.Root.AsArray("items");

        Assert.Equal(2, array.Count);

        array.AsTable(0);
        array.AsTable(1);
    }

    [Fact]
    public void Writer_Canonicalizes_Array()
    {
        var document = Toml.Parse("""
            a = [ 1,
            2, { x=3 }, ]

            """);

        Assert.Equal("a = [1, 2, {x = 3}]\n", Toml.Write(document));
    }

    [Fact]
    public void Writer_Preserves_Inline_Table_As_Value_Form()
    {
        var document = Toml.Parse("a = { b = 1, nested = { c = 2 } }\n");

        Assert.Equal("a = {b = 1, nested = {c = 2}}\n", Toml.Write(document));
    }

    [Fact]
    public void Programmatic_Array_Accepts_Heterogeneous_Nodes()
    {
        var document = new TomlDocument();

        var array = new TomlArray
        {
            TomlValue.FromInteger(1),
            TomlValue.FromString("two")
        };

        var table = new TomlTable();
        table.Set("three", TomlValue.FromInteger(3));
        array.Add(table);

        document.Root.Set("value", array);

        var text = Toml.Write(document);

        Assert.Equal("value = [1, \"two\", {three = 3}]\n", text);
        Assert.True(Toml.TryParse(text).IsSuccess);
    }
}

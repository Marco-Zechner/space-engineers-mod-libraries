using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlArrayOfTablesTests
{
    [Fact]
    public void Repeated_Array_Table_Headers_Append_Table_Elements()
    {
        var document = Toml.Parse("""
            [[people]]
            name = "one"
            [[people]]
            name = "two"

            """);

        var people = document.Root.AsArray("people");

        Assert.Equal(2, people.Count);

        var first = people.AsTable(0);
        var second = people.AsTable(1);
            
        Assert.Equal("one", first.AsValue("name").AsString());
        Assert.Equal("two", second.AsValue("name").AsString());
    }

    [Fact]
    public void Standard_Subtable_Attaches_To_Latest_Array_Element()
    {
        var document = Toml.Parse("""
            [[arr]]
            [arr.sub]
            value = 1
            [[arr]]
            [arr.sub]
            value = 2

            """);
            
        var arr = document.Root.AsArray("arr");

        Assert.Equal(2, arr.Count);

        var first = arr.AsTable(0);
        var firstSub = first.AsTable("sub");
        Assert.Equal(1, firstSub.AsValue("value").AsInteger());
            
        var second = arr.AsTable(1);
        var secondSub = second.AsTable("sub");
        Assert.Equal(2, secondSub.AsValue("value").AsInteger());
    }

    [Fact]
    public void Nested_Array_Of_Tables_Uses_Most_Recent_Parent_Element()
    {
        var document = Toml.Parse("""
            [[albums]]
            name = "first"
            [[albums.songs]]
            name = "a"
            [[albums.songs]]
            name = "b"
            [[albums]]
            name = "second"
            [[albums.songs]]
            name = "c"

            """);

        var albums = document.Root.AsArray("albums");

        var first = albums.AsTable(0);
        var second = albums.AsTable(1);

        Assert.Equal(2, first.AsArray("songs").Count);
        Assert.Equal(1, second.AsArray("songs").Count);
    }

    [Fact]
    public void Missing_Parent_Is_Implicit_Table_And_Can_Be_Opened_Later()
    {
        var document = Toml.Parse("""
            [[a.b]]
            x = 1
            [a]
            y = 2

            """);

        var a = document.Root.AsTable("a");

        Assert.Equal(2, a.Count);
        a.AsArray("b");
        Assert.Equal(2, a.AsValue("y").AsInteger());
    }

    [Fact]
    public void Implicit_Parent_From_Nested_Aot_Cannot_Later_Become_Aot() 
        => Assert.False(Toml.TryParse("""
            [[albums.songs]]
            name = "one"
            [[albums]]
            name = "two"

            """).IsSuccess);

    [Fact]
    public void Static_Array_Cannot_Become_Array_Of_Tables() 
        => Assert.False(Toml.TryParse("""
            fruit = []
            [[fruit]]

            """).IsSuccess);

    [Fact]
    public void Array_Of_Tables_Cannot_Become_Standard_Table() 
        => Assert.False(Toml.TryParse("""
            [[fruit]]
            [fruit]

            """).IsSuccess);

    [Fact]
    public void Standard_Table_Cannot_Become_Array_Of_Tables() 
        => Assert.False(Toml.TryParse("""
            [fruit]
            [[fruit]]

            """).IsSuccess);

    [Fact]
    public void Inline_Table_Cannot_Be_Extended_By_Array_Of_Tables() 
        => Assert.False(Toml.TryParse("""
            root = { child = {} }
            [[root.child]]

            """).IsSuccess);

    [Fact]
    public void Dotted_Key_Table_Cannot_Be_Reopened_As_Array_Of_Tables() 
        => Assert.False(Toml.TryParse("""
            [fruit]
            apple.color = "red"
            [[fruit.apple]]

            """).IsSuccess);

    [Fact]
    public void Array_Of_Tables_Cannot_Be_Extended_Through_Dotted_Key() 
        => Assert.False(Toml.TryParse("""
            [[a.b]]
            [a]
            b.y = 2

            """).IsSuccess);

    [Theory]
    [InlineData("[[a]\n")]
    [InlineData("[[a] x\n")]
    [InlineData("[[a\n")]
    [InlineData("[[]]\n")]
    public void Malformed_Array_Table_Headers_Are_Rejected(string text) 
        => Assert.False(Toml.TryParse(text).IsSuccess);

    [Fact]
    public void Empty_Quoted_Array_Table_Name_Is_Valid()
    {
        var document = Toml.Parse("""
            [['']]
            x = 1
            [['']]
            x = 2

            """);

        var array = document.Root.AsArray("");

        Assert.Equal(2, array.Count);
    }

    [Fact]
    public void Writer_Emits_Array_Of_Tables_Syntax()
    {
        var document = Toml.Parse("""
            [[products]]
            name = "Hammer"
            [[products]]
            name = "Nail"

            """);

        var canonical = Toml.Write(document);

        Assert.Equal("""
            [[products]]
            name = "Hammer"

            [[products]]
            name = "Nail"

            """.ReplaceLineEndings("\n"), canonical);

        var reparsed = Toml.Parse(canonical);

        Assert.Equal(2, reparsed.Root.AsArray("products").Count);
    }

    [Fact]
    public void Writer_Emits_Nested_Aot_And_Subtable_Paths()
    {
        var document = Toml.Parse("""
            [[a]]
            [[a.b]]
            [a.b.c]
            d = "first"
            [[a.b]]
            [a.b.c]
            d = "second"

            """);

        var canonical = Toml.Write(document);
        var reparsed = Toml.Parse(canonical);
        var a = reparsed.Root.AsArray("a");
        var firstA = a.AsTable(0);

        Assert.Equal(2, firstA.AsArray("b").Count);
    }

    [Fact]
    public void Programmatic_Array_Of_Tables_Remains_Static_Array_Syntax()
    {
        var document = new TomlDocument();
        var array = new TomlArray();
            
        var first = new TomlTable();
        first.Set("x", TomlValue.FromInteger(1));

        var second = new TomlTable();
        second.Set("x", TomlValue.FromInteger(2));

        array.Add(first);
        array.Add(second);

        document.Root.Set("items", array);

        Assert.Equal("items = [{x = 1}, {x = 2}]\n", Toml.Write(document));
    }
}

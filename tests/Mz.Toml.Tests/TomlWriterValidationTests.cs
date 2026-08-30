using System;
using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlWriterValidationTests
{
    [Fact]
    public void Writer_Rejects_Root_Table_Self_Cycle()
    {
        var document = new TomlDocument();
        document.Root.Set("self", document.Root);

        Assert.Throws<InvalidOperationException>(() => Toml.Write(document));
    }

    [Fact]
    public void Writer_Rejects_Child_Table_Self_Cycle()
    {
        var document = new TomlDocument();
        var child = new TomlTable();
        child.Set("self", child);
        document.Root.Set("child", child);

        Assert.Throws<InvalidOperationException>(() => Toml.Write(document));
    }

    [Fact]
    public void Writer_Rejects_Mutual_Table_Cycle()
    {
        var document = new TomlDocument();
        var left = new TomlTable();
        var right = new TomlTable();
        left.Set("right", right);
        right.Set("left", left);
        document.Root.Set("left", left);

        Assert.Throws<InvalidOperationException>(() => Toml.Write(document));
    }

    [Fact]
    public void Writer_Rejects_Array_Self_Cycle()
    {
        var document = new TomlDocument();
        var array = new TomlArray();
        array.Add(array);
        document.Root.Set("items", array);

        Assert.Throws<InvalidOperationException>(() => Toml.Write(document));
    }

    [Fact]
    public void Writer_Rejects_Mixed_Table_Array_Cycle()
    {
        var document = new TomlDocument();
        var table = new TomlTable();
        var array = new TomlArray();
        table.Set("items", array);
        array.Add(table);
        document.Root.Set("table", table);

        Assert.Throws<InvalidOperationException>(() => Toml.Write(document));
    }

    [Fact]
    public void Writer_Allows_Shared_Acyclic_Table_References()
    {
        var document = new TomlDocument();
        var shared = new TomlTable();
        shared.Set("x", TomlValue.FromInteger(1));
        document.Root.Set("left", shared);
        document.Root.Set("right", shared);

        Assert.Equal("""
            [left]
            x = 1

            [right]
            x = 1

            """,
            Toml.Write(document));
    }

    [Fact]
    public void Writer_Allows_Shared_Acyclic_Array_References()
    {
        var document = new TomlDocument();

        var shared = new TomlArray
        {
            TomlValue.FromInteger(1),
            TomlValue.FromInteger(2)
        };

        document.Root.Set("left", shared);
        document.Root.Set("right", shared);

        Assert.Equal("""
            left = [1, 2]
            right = [1, 2]

            """,
            Toml.Write(document));
    }

    [Fact]
    public void Writer_Allows_Diamond_Shaped_Acyclic_Graph()
    {
        var document = new TomlDocument();
        var leaf = new TomlTable();
        leaf.Set("value", TomlValue.FromInteger(1));

        var left = new TomlTable();
        left.Set("leaf", leaf);

        var right = new TomlTable();
        right.Set("leaf", leaf);

        document.Root.Set("left", left);
        document.Root.Set("right", right);

        var written = Toml.Write(document);

        Assert.Equal("""
            [left]

            [left.leaf]
            value = 1

            [right]

            [right.leaf]
            value = 1

            """,
            written);

        Assert.True(Toml.TryParse(written).IsSuccess);
    }
}
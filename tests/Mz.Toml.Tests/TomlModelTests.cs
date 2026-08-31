using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlModelTests
{
    [Fact]
    public void Keys_Are_ReadOnly_And_Keep_Insertion_Order()
    {
        var table = new TomlTable();

        table.Set("second", TomlValue.FromInteger(2));
        table.Set("first", TomlValue.FromInteger(1));

        Assert.Equal(["second", "first"], table.Keys);
        Assert.False(table.Keys is ICollection<string>);

        table.Set("third", TomlValue.FromInteger(3));
        Assert.Equal(["second", "first", "third"], table.Keys);
    }

    [Fact]
    public void Table_Enumeration_Rejects_Structural_Mutation()
    {
        var table = new TomlTable();

        table.Set("first", TomlValue.FromInteger(1));
        table.Set("second", TomlValue.FromInteger(2));

        var enumerator = table.GetEnumerator();

        Assert.True(enumerator.MoveNext());

        table.Set("third", TomlValue.FromInteger(3));

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void Empty_Programmatic_Key_Is_Allowed_And_Quoted()
    {
        var document = new TomlDocument();

        document.Root.Set("", TomlValue.FromInteger(1));

        Assert.Equal("\"\" = 1\n", Toml.Write(document));
    }

    [Fact]
    public void Parse_Diagnostics_Do_Not_Expose_Mutable_Collection()
    {
        var result = Toml.TryParse("broken\n");

        Assert.False(result.Diagnostics is ICollection<TomlDiagnostic>);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(TomlDiagnosticCode.MissingEquals, diagnostic.Code);
    }
}

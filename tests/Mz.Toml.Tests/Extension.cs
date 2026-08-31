using Xunit;

namespace Mz.Toml.Tests;

public static class Extension
{
    public static TomlArray AsArray(this TomlArray table, int index) => Assert.IsType<TomlArray>(table[index]);
    public static TomlTable AsTable(this TomlArray table, int index) => Assert.IsType<TomlTable>(table[index]);
    public static TomlValue AsValue(this TomlArray table, int index) => Assert.IsType<TomlValue>(table[index]);
    public static TomlArray AsArray(this TomlTable table, string key) => Assert.IsType<TomlArray>(table[key]);
    public static TomlTable AsTable(this TomlTable table, string key) => Assert.IsType<TomlTable>(table[key]);
    public static TomlValue AsValue(this TomlTable table, string key) => Assert.IsType<TomlValue>(table[key]);

}
 
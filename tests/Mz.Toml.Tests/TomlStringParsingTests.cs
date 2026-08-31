using System;
using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlStringParsingTests
{
    [Fact]
    public void Parses_Single_Line_Literal_Strings()
    {
        var document = Toml.Parse("""
            path = 'C:\Users\name'
            end = 'String ends here\'
            empty = ''

            """);

        Assert.Equal(@"C:\Users\name", document.Root.AsValue("path").AsString());
        Assert.Equal(@"String ends here\", document.Root.AsValue("end").AsString());
        Assert.Equal("", document.Root.AsValue("empty").AsString());
    }

    [Fact]
    public void Multiline_Basic_String_Trims_First_Newline_And_Folds()
    {
        var document = Toml.Parse(""""
            value = """
            The quick brown \

             
              fox jumps over \
                the lazy dog."""

            """");

        Assert.Equal("The quick brown fox jumps over the lazy dog.", document.Root.AsValue("value").AsString());
    }

    [Fact]
    public void Multiline_Basic_Continuation_Allows_Whitespace_After_Backslash()
    {
        var document = Toml.Parse(""""
            value = """\
            The quick brown \
            fox jumps over \   
            the lazy dog.\	
            """

            """");

        Assert.Equal("The quick brown fox jumps over the lazy dog.", document.Root.AsValue("value").AsString());
    }

    [Fact]
    public void Multiline_Basic_Preserves_Whitespace_Before_Backslash()
    {
        // not a raw string literal, because we want to test tabs
        var document = Toml.Parse(
            "value = \"\"\"a   \t\\\n" +
            "   b\"\"\"\n");

        Assert.Equal("a   \tb", document.Root.AsValue("value").AsString());
    }

    [Fact]
    public void Multiline_Basic_Normalizes_Crlf_To_Lf()
    {
        // not a raw string literal because this test specifically exercises CRLF input
        var document = Toml.Parse(
            "value = \"\"\"\r\n" +
            "one\r\n" +
            "two\"\"\"\r\n");

        Assert.Equal("one\ntwo", document.Root.AsValue("value").AsString());
    }

    [Fact]
    public void Multiline_Basic_Crlf_Continuation_Is_Trimmed()
    {
        // not a raw string literal because this test specifically exercises CRLF input
        var document = Toml.Parse(
            "value = \"\"\"\\\r\n" +
            "\"\"\"\r\n");

        Assert.Equal("", document.Root.AsValue("value").AsString());
    }

    [Fact]
    public void Multiline_Literal_Preserves_Content_And_Normalizes_Newlines()
    {
        // not a raw string literal because this test specifically exercises CRLF input
        var document = Toml.Parse(
            "value = '''\r\n" +
            "first\\n\r\n" +
            "second\\u0041'''\r\n");

        Assert.Equal("first\\n\nsecond\\u0041", document.Root.AsValue("value").AsString());
    }

    [Fact]
    public void Multiline_Strings_Allow_One_Or_Two_Quote_Characters()
    {
        var document = Toml.Parse(""""""
            basic1 = """"one""""
            basic2 = """""two"""""
            literal1 = ''''one''''
            literal2 = '''''two'''''

            """""");

        Assert.Equal("\"one\"", document.Root.AsValue("basic1").AsString());
        Assert.Equal("\"\"two\"\"", document.Root.AsValue("basic2").AsString());
        Assert.Equal("'one'", document.Root.AsValue("literal1").AsString());
        Assert.Equal("''two''", document.Root.AsValue("literal2").AsString());
    }

    [Fact]
    public void Multiline_Closing_Delimiter_Can_Have_One_Or_Two_Quotes()
    {
        var document = Toml.Parse(""""""
            four = """
            four
            """"
            five = """
            five
            """""

            """""");

        Assert.Equal("four\n\"", document.Root.AsValue("four").AsString());
        Assert.Equal("five\n\"\"", document.Root.AsValue("five").AsString());
    }

    [Fact]
    public void Basic_And_Multiline_Basic_Use_Unicode_Escapes()
    {
        var document = Toml.Parse(""""
            single = "\u03B4 \U00010AF1 \u0000"
            multi = """\u03B4 \U00010AF1 \u0000"""

            """");

        var expected = $"\u03B4 {char.ConvertFromUtf32(0x10AF1)} \0";

        Assert.Equal(expected, document.Root.AsValue("single").AsString());
        Assert.Equal(expected, document.Root.AsValue("multi").AsString());
    }

    [Theory]
    [InlineData("value = \"\\x33\"\n")]
    [InlineData("value = \"\\@\"\n")]
    [InlineData("value = \"\\/\"\n")]
    [InlineData("value = \"\"\"t\\a\"\"\"\n")]
    [InlineData("value = \"\"\"t\\ t\"\"\"\n")]
    [InlineData("value = \"\\uD801\"\n")]
    [InlineData("value = \"\\UFFFFFFFF\"\n")]
    [InlineData("value = \"\\U80000000\"\n")]
    [InlineData("value = \"\\U00110000\"\n")]
    [InlineData("value = \"\\U0000D800\"\n")]
    public void Toml_10_Invalid_Escapes_Are_Rejected(string text)
    {
        var result = Toml.TryParse(text);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Single_Line_Strings_Reject_Raw_Newline()
    {
        Assert.False(Toml.TryParse("""
            a = "first
            second"

            """).IsSuccess);
        Assert.False(Toml.TryParse("""
            a = 'first
            second'

            """).IsSuccess);
    }

    [Fact]
    public void Multiline_Continuation_Requires_A_Newline()
    {
        Assert.False(Toml.TryParse(""""
            a = """foo \   bar"""

            """").IsSuccess);
    }

    [Fact]
    public void Six_Unescaped_Closing_Quotes_Are_Rejected()
    {
        Assert.False(Toml.TryParse("a = \"\"\"six \"\"\"\"\"\"\n").IsSuccess);
        Assert.False(Toml.TryParse("a = '''six ''''''\n").IsSuccess);
    }

    [Fact]
    public void Quoted_Keys_Remain_Single_Line()
    {
        Assert.False(Toml.TryParse("\"\"\"key\"\"\" = 1\n").IsSuccess);
        Assert.False(Toml.TryParse("'''key''' = 1\n").IsSuccess);
    }

    [Fact]
    public void Raw_Control_Characters_Are_Rejected()
    {
        var text = $"value = \"a{(char)1}b\"\n";
        Assert.False(Toml.TryParse(text).IsSuccess);

        text = $"value = 'a{(char)1}b'\n";
        Assert.False(Toml.TryParse(text).IsSuccess);
    }

    [Fact]
    public void Parser_Rejects_Unpaired_Raw_Surrogate()
    {
        const string text = "value = \"a\uD800b\"\n";
        Assert.False(Toml.TryParse(text).IsSuccess);
    }

    [Fact]
    public void Writer_Canonicalizes_All_String_Forms_To_Basic_String()
    {
        var document = Toml.Parse(""""
            literal = 'a\b'
            multi = """
            one
            two"""

            """");

        var written = Toml.Write(document);

        Assert.Equal("""
            literal = "a\\b"
            multi = "one\ntwo"

            """.ReplaceLineEndings("\n"),
            written);

        var reparsed = Toml.Parse(written);

        Assert.Equal("a\\b", reparsed.Root.AsValue("literal").AsString());

        Assert.Equal("one\ntwo", reparsed.Root.AsValue("multi").AsString());
    }

    [Fact]
    public void Writer_Rejects_Unpaired_Utf16_Surrogate()
    {
        var document = new TomlDocument();

        document.Root.Set("bad", TomlValue.FromString(new string(['\uD800' ])));
        
        Assert.Throws<InvalidOperationException>(() => Toml.Write(document));
    }

    [Fact]
    public void Writer_Preserves_Valid_Surrogate_Pair()
    {
        var document = new TomlDocument();
        var value = char.ConvertFromUtf32(0x10AF1);
        
        document.Root.Set("value", TomlValue.FromString(value));
        var reparsed = Toml.Parse(Toml.Write(document));

        Assert.Equal(value, reparsed.Root.AsValue("value").AsString());
    }
}

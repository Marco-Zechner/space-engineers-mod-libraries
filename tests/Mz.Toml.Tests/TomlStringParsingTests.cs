using System;
using Mz.Toml;
using Xunit;

namespace Mz.Toml.Tests
{
    public sealed class TomlStringParsingTests
    {
        [Fact]
        public void Parses_Single_Line_Literal_Strings()
        {
            var document = Toml.Parse(
                "path = 'C:\\Users\\name'\n" +
                "end = 'String ends here\\'\n" +
                "empty = ''\n");

            Assert.Equal(
                "C:\\Users\\name",
                Value(document.Root, "path").AsString());

            Assert.Equal(
                "String ends here\\",
                Value(document.Root, "end").AsString());

            Assert.Equal(
                "",
                Value(document.Root, "empty").AsString());
        }

        [Fact]
        public void Multiline_Basic_String_Trims_First_Newline_And_Folds()
        {
            var document = Toml.Parse(
                "value = \"\"\"\n" +
                "The quick brown \\\n" +
                "\n" +
                "\n" +
                "  fox jumps over \\\n" +
                "    the lazy dog.\"\"\"\n");

            Assert.Equal(
                "The quick brown fox jumps over the lazy dog.",
                Value(document.Root, "value").AsString());
        }

        [Fact]
        public void Multiline_Basic_Continuation_Allows_Whitespace_After_Backslash()
        {
            var document = Toml.Parse(
                "value = \"\"\"\\\n" +
                "The quick brown \\\n" +
                "fox jumps over \\   \n" +
                "the lazy dog.\\\t\n" +
                "\"\"\"\n");

            Assert.Equal(
                "The quick brown fox jumps over the lazy dog.",
                Value(document.Root, "value").AsString());
        }

        [Fact]
        public void Multiline_Basic_Preserves_Whitespace_Before_Backslash()
        {
            var document = Toml.Parse(
                "value = \"\"\"a   \t\\\n" +
                "   b\"\"\"\n");

            Assert.Equal(
                "a   \tb",
                Value(document.Root, "value").AsString());
        }

        [Fact]
        public void Multiline_Basic_Normalizes_Crlf_To_Lf()
        {
            var document = Toml.Parse(
                "value = \"\"\"\r\n" +
                "one\r\n" +
                "two\"\"\"\r\n");

            Assert.Equal(
                "one\ntwo",
                Value(document.Root, "value").AsString());
        }

        [Fact]
        public void Multiline_Basic_Crlf_Continuation_Is_Trimmed()
        {
            var document = Toml.Parse(
                "value = \"\"\"\\\r\n" +
                "\"\"\"\r\n");

            Assert.Equal(
                "",
                Value(document.Root, "value").AsString());
        }

        [Fact]
        public void Multiline_Literal_Preserves_Content_And_Normalizes_Newlines()
        {
            var document = Toml.Parse(
                "value = '''\r\n" +
                "first\\n\r\n" +
                "second\\u0041'''\r\n");

            Assert.Equal(
                "first\\n\nsecond\\u0041",
                Value(document.Root, "value").AsString());
        }

        [Fact]
        public void Multiline_Strings_Allow_One_Or_Two_Quote_Characters()
        {
            var document = Toml.Parse(
                "basic1 = \"\"\"\"one\"\"\"\"\n" +
                "basic2 = \"\"\"\"\"two\"\"\"\"\"\n" +
                "literal1 = ''''one''''\n" +
                "literal2 = '''''two'''''\n");

            Assert.Equal(
                "\"one\"",
                Value(document.Root, "basic1").AsString());

            Assert.Equal(
                "\"\"two\"\"",
                Value(document.Root, "basic2").AsString());

            Assert.Equal(
                "'one'",
                Value(document.Root, "literal1").AsString());

            Assert.Equal(
                "''two''",
                Value(document.Root, "literal2").AsString());
        }

        [Fact]
        public void Multiline_Closing_Delimiter_Can_Have_One_Or_Two_Quotes()
        {
            var document = Toml.Parse(
                "four = \"\"\"\n" +
                "four\n" +
                "\"\"\"\"\n" +
                "five = \"\"\"\n" +
                "five\n" +
                "\"\"\"\"\"\n");

            Assert.Equal(
                "four\n\"",
                Value(document.Root, "four").AsString());

            Assert.Equal(
                "five\n\"\"",
                Value(document.Root, "five").AsString());
        }

        [Fact]
        public void Basic_And_Multiline_Basic_Use_Unicode_Escapes()
        {
            var document = Toml.Parse(
                "single = \"\\u03B4 \\U00010AF1 \\u0000\"\n" +
                "multi = \"\"\"\\u03B4 \\U00010AF1 \\u0000\"\"\"\n");

            var expected =
                "\u03B4 " +
                char.ConvertFromUtf32(0x10AF1) +
                " \0";

            Assert.Equal(
                expected,
                Value(document.Root, "single").AsString());

            Assert.Equal(
                expected,
                Value(document.Root, "multi").AsString());
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
        public void Toml_10_Invalid_Escapes_Are_Rejected(
            string text)
        {
            var result = Toml.TryParse(text);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Single_Line_Strings_Reject_Raw_Newline()
        {
            Assert.False(
                Toml.TryParse(
                    "a = \"first\nsecond\"\n").IsSuccess);

            Assert.False(
                Toml.TryParse(
                    "a = 'first\nsecond'\n").IsSuccess);
        }

        [Fact]
        public void Multiline_Continuation_Requires_A_Newline()
        {
            Assert.False(
                Toml.TryParse(
                    "a = \"\"\"foo \\   bar\"\"\"\n").IsSuccess);
        }

        [Fact]
        public void Six_Unescaped_Closing_Quotes_Are_Rejected()
        {
            Assert.False(
                Toml.TryParse(
                    "a = \"\"\"six \"\"\"\"\"\"\n").IsSuccess);

            Assert.False(
                Toml.TryParse(
                    "a = '''six ''''''\n").IsSuccess);
        }

        [Fact]
        public void Quoted_Keys_Remain_Single_Line()
        {
            Assert.False(
                Toml.TryParse(
                    "\"\"\"key\"\"\" = 1\n").IsSuccess);

            Assert.False(
                Toml.TryParse(
                    "'''key''' = 1\n").IsSuccess);
        }

        [Fact]
        public void Raw_Control_Characters_Are_Rejected()
        {
            var text =
                "value = \"a" +
                ((char)1) +
                "b\"\n";

            Assert.False(
                Toml.TryParse(text).IsSuccess);

            text =
                "value = 'a" +
                ((char)1) +
                "b'\n";

            Assert.False(
                Toml.TryParse(text).IsSuccess);
        }

        [Fact]
        public void Parser_Rejects_Unpaired_Raw_Surrogate()
        {
            var text =
                "value = \"a" +
                '\uD800' +
                "b\"\n";

            Assert.False(
                Toml.TryParse(text).IsSuccess);
        }

        [Fact]
        public void Writer_Canonicalizes_All_String_Forms_To_Basic_String()
        {
            var document = Toml.Parse(
                "literal = 'a\\b'\n" +
                "multi = \"\"\"\n" +
                "one\n" +
                "two\"\"\"\n");

            var written = Toml.Write(document);

            Assert.Equal(
                "literal = \"a\\\\b\"\n" +
                "multi = \"one\\ntwo\"\n",
                written);

            var reparsed = Toml.Parse(written);

            Assert.Equal(
                "a\\b",
                Value(reparsed.Root, "literal").AsString());

            Assert.Equal(
                "one\ntwo",
                Value(reparsed.Root, "multi").AsString());
        }

        [Fact]
        public void Writer_Rejects_Unpaired_Utf16_Surrogate()
        {
            var document = new TomlDocument();

            document.Root.Set(
                "bad",
                TomlValue.FromString(
                    new string(
                        new[] { '\uD800' })));

            Assert.Throws<InvalidOperationException>(
                () => Toml.Write(document));
        }

        [Fact]
        public void Writer_Preserves_Valid_Surrogate_Pair()
        {
            var document = new TomlDocument();

            var value =
                char.ConvertFromUtf32(0x10AF1);

            document.Root.Set(
                "value",
                TomlValue.FromString(value));

            var reparsed =
                Toml.Parse(
                    Toml.Write(document));

            Assert.Equal(
                value,
                Value(
                    reparsed.Root,
                    "value").AsString());
        }

        private static TomlValue Value(
            TomlTable table,
            string key)
        {
            return Assert.IsType<TomlValue>(
                table[key]);
        }
    }
}
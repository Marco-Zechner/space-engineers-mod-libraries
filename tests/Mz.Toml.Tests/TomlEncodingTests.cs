using System;
using System.Text;
using Mz.Toml;
using Xunit;

namespace Mz.Toml.Tests
{
    public sealed class TomlEncodingTests
    {
        [Fact]
        public void TryParse_Bytes_Accepts_Valid_Utf8()
        {
            var bytes =
                new UTF8Encoding(false)
                    .GetBytes(
                        "name = \"MÃ¤ÃŸig æ—¥æœ¬èªž\"\n");

            var result =
                Toml.TryParse(bytes);

            Assert.True(result.IsSuccess);

            Assert.Equal(
                "MÃ¤ÃŸig æ—¥æœ¬èªž",
                Assert.IsType<TomlValue>(
                    result.Document.Root["name"])
                    .AsString());
        }

        [Fact]
        public void TryParse_Bytes_Accepts_Single_Leading_Utf8_Bom()
        {
            var payload =
                new UTF8Encoding(false)
                    .GetBytes(
                        "value = 1\n");

            var bytes =
                new byte[payload.Length + 3];

            bytes[0] = 0xEF;
            bytes[1] = 0xBB;
            bytes[2] = 0xBF;

            Array.Copy(
                payload,
                0,
                bytes,
                3,
                payload.Length);

            var result =
                Toml.TryParse(bytes);

            Assert.True(result.IsSuccess);

            Assert.Equal(
                1,
                Assert.IsType<TomlValue>(
                    result.Document.Root["value"])
                    .AsInteger());
        }

        [Fact]
        public void TryParse_Bytes_Rejects_Invalid_Utf8()
        {
            var bytes =
                new byte[]
                {
                    (byte)'x',
                    (byte)' ',
                    (byte)'=',
                    (byte)' ',
                    (byte)'"',
                    0xC3,
                    (byte)'"'
                };

            var result =
                Toml.TryParse(bytes);

            Assert.False(result.IsSuccess);
            Assert.Single(result.Diagnostics);

            Assert.Equal(
                TomlDiagnosticCode.InvalidEncoding,
                result.Diagnostics[0].Code);
        }

        [Fact]
        public void TryParse_Bytes_Rejects_Utf8_Encoded_Surrogate()
        {
            var bytes =
                new byte[]
                {
                    (byte)'#',
                    (byte)' ',
                    0xED,
                    0xA0,
                    0x80
                };

            var result =
                Toml.TryParse(bytes);

            Assert.False(result.IsSuccess);

            Assert.Equal(
                TomlDiagnosticCode.InvalidEncoding,
                result.Diagnostics[0].Code);
        }

        [Fact]
        public void TryParse_Bytes_Rejects_Truncated_Utf8()
        {
            var bytes =
                new byte[]
                {
                    (byte)'#',
                    (byte)' ',
                    0xC3
                };

            var result =
                Toml.TryParse(bytes);

            Assert.False(result.IsSuccess);

            Assert.Equal(
                TomlDiagnosticCode.InvalidEncoding,
                result.Diagnostics[0].Code);
        }

        [Fact]
        public void Second_Utf8_Bom_Is_Not_Treated_As_Encoding_Preamble()
        {
            var bytes =
                new byte[]
                {
                    0xEF,
                    0xBB,
                    0xBF,
                    0xEF,
                    0xBB,
                    0xBF,
                    (byte)'x',
                    (byte)'=',
                    (byte)'1',
                    (byte)'\n'
                };

            var result =
                Toml.TryParse(bytes);

            Assert.False(result.IsSuccess);
            Assert.NotEqual(
                TomlDiagnosticCode.InvalidEncoding,
                result.Diagnostics[0].Code);
        }

        [Fact]
        public void Literal_Replacement_Character_Remains_Valid_Unicode()
        {
            var bytes =
                new UTF8Encoding(false)
                    .GetBytes(
                        "value = \"\uFFFD\"\n");

            var result =
                Toml.TryParse(bytes);

            Assert.True(result.IsSuccess);

            Assert.Equal(
                "\uFFFD",
                Assert.IsType<TomlValue>(
                    result.Document.Root["value"])
                    .AsString());
        }

        [Fact]
        public void Parse_Bytes_Throws_TomlParseException_For_Invalid_Utf8()
        {
            var bytes =
                new byte[]
                {
                    0xC3
                };

            var exception =
                Assert.Throws<TomlParseException>(
                    () => Toml.Parse(bytes));

            Assert.Equal(
                TomlDiagnosticCode.InvalidEncoding,
                exception.Diagnostic.Code);
        }

        [Fact]
        public void Null_Byte_Input_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => Toml.TryParse(
                    (byte[])null!));

            Assert.Throws<ArgumentNullException>(
                () => Toml.Parse(
                    (byte[])null!));
        }

        [Fact]
        public void String_Api_Remains_Already_Decoded_Unicode_API()
        {
            var result =
                Toml.TryParse(
                    "value = \"\uFFFD\"\n");

            Assert.True(result.IsSuccess);
        }
    }
}
using System;
using Xunit;

namespace Mz.Logging.Tests
{
    public sealed class PlainTextLogFormatterTests
    {
        [Theory]
        [InlineData(LogLevel.Trace, "TRACE")]
        [InlineData(LogLevel.Debug, "DEBUG")]
        [InlineData(LogLevel.Information, "INFO")]
        [InlineData(LogLevel.Warning, "WARN")]
        [InlineData(LogLevel.Error, "ERROR")]
        [InlineData(LogLevel.Critical, "CRITICAL")]
        public void Format_UsesExpectedLevelName(
            LogLevel level,
            string expectedName
        )
        {
            var formatter = new PlainTextLogFormatter();

            var entry = new LogEntry(
                new DateTime(
                    2026,
                    7,
                    19,
                    18,
                    30,
                    0,
                    DateTimeKind.Utc
                ),
                level,
                "CommandAPI",
                "Example message.",
                null
            );

            string result = formatter.Format(entry);

            Assert.Equal(
                "2026-07-19T18:30:00.000Z "
                + "["
                + expectedName
                + "] [CommandAPI] Example message.",
                result
            );
        }

        [Fact]
        public void Format_Exception_AppendsExceptionText()
        {
            var formatter = new PlainTextLogFormatter();

            var exception = new InvalidOperationException(
                "Example failure."
            );

            var entry = new LogEntry(
                new DateTime(
                    2026,
                    7,
                    19,
                    18,
                    30,
                    0,
                    DateTimeKind.Utc
                ),
                LogLevel.Error,
                "CommandAPI",
                "Command failed.",
                exception
            );

            string result = formatter.Format(entry);

            string expected =
                "2026-07-19T18:30:00.000Z "
                + "[ERROR] [CommandAPI] Command failed."
                + Environment.NewLine
                + exception;

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Format_PreservesMessageWhitespaceAndLineBreaks()
        {
            var formatter = new PlainTextLogFormatter();

            var entry = new LogEntry(
                new DateTime(
                    1970,
                    1,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc
                ),
                LogLevel.Information,
                "CommandAPI",
                " first line\nsecond line ",
                null
            );

            string result = formatter.Format(entry);

            Assert.EndsWith(
                "[INFO] [CommandAPI]  first line\nsecond line ",
                result
            );
        }

        [Fact]
        public void Format_NullEntry_ThrowsArgumentNullException()
        {
            var formatter = new PlainTextLogFormatter();

            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    formatter.Format(null!);
                }
            );
        }
    }
}

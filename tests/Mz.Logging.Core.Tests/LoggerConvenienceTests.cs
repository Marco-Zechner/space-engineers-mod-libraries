using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.Logging.Tests
{
    public sealed class LoggerConvenienceTests
    {
        [Fact]
        public void ConvenienceMethods_WriteExpectedLevels()
        {
            var sink = new RecordingSink();

            var logger = new Logger("CommandAPI", sink, LogLevel.Trace);

            logger.Trace("Trace message.");
            logger.Debug("Debug message.");
            logger.Info("Information message.");
            logger.Warning("Warning message.");
            logger.Error("Error message.");
            logger.Critical("Critical message.");

            Assert.Collection(
                sink.Entries,
                entry => AssertEntry(entry, LogLevel.Trace, "Trace message."),
                entry => AssertEntry(entry, LogLevel.Debug, "Debug message."),
                entry => AssertEntry(entry, LogLevel.Information, "Information message."),
                entry => AssertEntry(entry, LogLevel.Warning, "Warning message."),
                entry => AssertEntry(entry, LogLevel.Error, "Error message."),
                entry => AssertEntry(entry, LogLevel.Critical, "Critical message.")
            );
        }

        [Fact]
        public void ConvenienceMethod_ForwardsException()
        {
            var sink = new RecordingSink();

            var logger = new Logger("CommandAPI", sink, LogLevel.Trace);

            var exception = new InvalidOperationException("Example failure");

            logger.Error("Command execution failed.", exception);

            var entry = Assert.Single(sink.Entries);

            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Equal("Command execution failed.", entry.Message);

            Assert.Same(exception, entry.Exception);
        }

        [Fact]
        public void ConvenienceMethod_StillUsesMinimumLevel()
        {
            var sink = new RecordingSink();

            var logger = new Logger("CommandAPI", sink, LogLevel.Warning);

            logger.Trace("Filtered trace.");
            logger.Debug("Filtered debug.");
            logger.Info("Filtered information.");
            logger.Warning("Written warning.");

            var entry = Assert.Single(sink.Entries);

            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.Equal("Written warning.", entry.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void IsEnabled_UsesConfiguredThreshold(int numericLevel)
        {
            var sink = new RecordingSink();

            var logger = new Logger("CommandAPI", sink, LogLevel.Warning);

            var level = (LogLevel)numericLevel;
            var expected = level >= LogLevel.Warning;

            Assert.Equal(expected, logger.IsEnabled(level));
        }

        private static void AssertEntry(LogEntry entry, LogLevel expectedLevel, string expectedMessage)
        {
            Assert.Equal(expectedLevel, entry.Level);
            Assert.Equal(expectedMessage, entry.Message);
        }

        private sealed class RecordingSink : ILogSink
        {
            public List<LogEntry> Entries { get; } = [];

            public void Write(LogEntry entry) => Entries.Add(entry);
        }
    }
}
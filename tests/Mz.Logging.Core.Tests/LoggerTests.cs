using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.Logging.Tests
{
    public sealed class LoggerTests
    {
        [Fact]
        public void Constructor_StoresNormalizedSourceAndMinimumLevel()
        {
            var sink = new RecordingSink();

            var logger = new Logger("  CommandAPI  ", sink, LogLevel.Warning);

            Assert.Equal("CommandAPI", logger.Source);
            Assert.Equal(LogLevel.Warning, logger.MinimumLevel);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidSource_ThrowsArgumentException(string? source)
        {
            var sink = new RecordingSink();

            Assert.ThrowsAny<ArgumentException>(() => new Logger(source!, sink, LogLevel.Information));
        }

        [Fact]
        public void Constructor_NullSink_ThrowsArgumentNullException() 
            => Assert.Throws<ArgumentNullException>(() => new Logger("CommandAPI", null!, LogLevel.Information));

        [Fact]
        public void Constructor_NullClock_ThrowsArgumentNullException()
        {
            var sink = new RecordingSink();

            Assert.Throws<ArgumentNullException>(() => new Logger("CommandAPI", sink, LogLevel.Information, null!));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(6)]
        [InlineData(100)]
        public void Constructor_InvalidMinimumLevel_Throws(int numericLevel)
        {
            var sink = new RecordingSink();

            Assert.Throws<ArgumentException>(() => new Logger("CommandAPI", sink, (LogLevel)numericLevel));
        }

        [Fact]
        public void Write_EnabledLevel_DispatchesCompleteEntry()
        {
            var sink = new RecordingSink();

            var timestamp = new DateTime(2026, 7, 19, 18, 30, 0, DateTimeKind.Utc);

            var expectedException = new InvalidOperationException("Example failure");

            var logger = new Logger("CommandAPI", sink, LogLevel.Information, () => timestamp);

            logger.Write(LogLevel.Error, "Command failed.", expectedException);

            var entry = Assert.Single(sink.Entries);

            Assert.Equal(timestamp, entry.TimestampUtc);

            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Equal("CommandAPI", entry.Source);
            Assert.Equal("Command failed.", entry.Message);
            Assert.Same(expectedException, entry.Exception);
        }

        [Fact]
        public void Write_LevelEqualToMinimum_DispatchesEntry()
        {
            var sink = new RecordingSink();

            var logger = new Logger("CommandAPI", sink, LogLevel.Warning);

            logger.Write(LogLevel.Warning, "Warning message.");

            Assert.Single(sink.Entries);
        }

        [Fact]
        public void Write_LevelBelowMinimum_DoesNotDispatchOrReadClock()
        {
            var sink = new RecordingSink();
            var clockWasRead = false;

            var logger = new Logger("CommandAPI", sink, LogLevel.Warning, () => {
                clockWasRead = true;
                return DateTime.UtcNow;
            });
            
            logger.Write(LogLevel.Information, "Filtered message.");

            Assert.Empty(sink.Entries);
            Assert.False(clockWasRead);
        }

        [Fact]
        public void Write_NullMessage_ThrowsArgumentNullException()
        {
            var sink = new RecordingSink();

            var logger = new Logger("CommandAPI", sink, LogLevel.Information);

            Assert.Throws<ArgumentNullException>(() => logger.Write(LogLevel.Information, null!));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(6)]
        public void Write_InvalidLevel_ThrowsArgumentException(int numericLevel)
        {
            var sink = new RecordingSink();

            var logger = new Logger("CommandAPI", sink, LogLevel.Information);

            Assert.Throws<ArgumentException>(() => logger.Write((LogLevel)numericLevel, "Message"));
        }

        [Fact]
        public void MinimumLevel_CanBeChangedAtRuntime()
        {
            var sink = new RecordingSink();

            var logger = new Logger("CommandAPI", sink, LogLevel.Warning);

            logger.Write(LogLevel.Information, "Initially filtered.");

            logger.MinimumLevel = LogLevel.Information;

            logger.Write(LogLevel.Information, "Now enabled.");

            var entry = Assert.Single(sink.Entries);
            Assert.Equal("Now enabled.", entry.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(6)]
        public void MinimumLevel_InvalidValue_Throws(int numericLevel)
        {
            var sink = new RecordingSink();

            var logger = new Logger("CommandAPI", sink, LogLevel.Information);

            Assert.Throws<ArgumentException>(() => logger.MinimumLevel = (LogLevel)numericLevel);
        }

        private sealed class RecordingSink : ILogSink
        {
            public List<LogEntry> Entries { get; } = new();

            public void Write(LogEntry entry) => Entries.Add(entry);
        }
    }
}

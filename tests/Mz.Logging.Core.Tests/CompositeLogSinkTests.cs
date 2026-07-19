using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.Logging.Tests
{
    public sealed class CompositeLogSinkTests
    {
        [Fact]
        public void Constructor_NullArray_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    new CompositeLogSink(null!);
                }
            );
        }

        [Fact]
        public void Constructor_EmptyArray_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(
                delegate
                {
                    new CompositeLogSink();
                }
            );
        }

        [Fact]
        public void Constructor_NullElement_ThrowsArgumentException()
        {
            var sink = new RecordingSink();

            Assert.Throws<ArgumentException>(
                delegate
                {
                    new CompositeLogSink(
                        sink,
                        null!
                    );
                }
            );
        }

        [Fact]
        public void Write_DispatchesSameEntryToEverySink()
        {
            var first = new RecordingSink();
            var second = new RecordingSink();

            var composite = new CompositeLogSink(
                first,
                second
            );

            var entry = CreateEntry();

            composite.Write(entry);

            Assert.Same(
                entry,
                Assert.Single(first.Entries)
            );

            Assert.Same(
                entry,
                Assert.Single(second.Entries)
            );
        }

        [Fact]
        public void Write_DispatchesInRegistrationOrder()
        {
            var calls = new List<string>();

            var first = new CallbackSink(
                delegate
                {
                    calls.Add("first");
                }
            );

            var second = new CallbackSink(
                delegate
                {
                    calls.Add("second");
                }
            );

            var third = new CallbackSink(
                delegate
                {
                    calls.Add("third");
                }
            );

            var composite = new CompositeLogSink(
                first,
                second,
                third
            );

            composite.Write(CreateEntry());

            Assert.Equal(
                new[]
                {
                    "first",
                    "second",
                    "third"
                },
                calls
            );
        }

        [Fact]
        public void Constructor_CopiesSuppliedSinkArray()
        {
            var original = new RecordingSink();
            var replacement = new RecordingSink();

            ILogSink[] sinks =
            {
                original
            };

            var composite = new CompositeLogSink(sinks);

            sinks[0] = replacement;

            composite.Write(CreateEntry());

            Assert.Single(original.Entries);
            Assert.Empty(replacement.Entries);
        }

        [Fact]
        public void Write_NullEntry_ThrowsArgumentNullException()
        {
            var sink = new RecordingSink();
            var composite = new CompositeLogSink(sink);

            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    composite.Write(null!);
                }
            );
        }

        [Fact]
        public void Write_SinkFailurePropagatesAndStopsDispatch()
        {
            var calls = new List<string>();

            var failing = new CallbackSink(
                delegate
                {
                    calls.Add("failing");

                    throw new InvalidOperationException(
                        "Sink failed."
                    );
                }
            );

            var later = new CallbackSink(
                delegate
                {
                    calls.Add("later");
                }
            );

            var composite = new CompositeLogSink(
                failing,
                later
            );

            Assert.Throws<InvalidOperationException>(
                delegate
                {
                    composite.Write(CreateEntry());
                }
            );

            Assert.Equal(
                new[]
                {
                    "failing"
                },
                calls
            );
        }

        private static LogEntry CreateEntry()
        {
            return new LogEntry(
                DateTimeOffset.UtcNow,
                LogLevel.Information,
                "CommandAPI",
                "Example message.",
                null
            );
        }

        private sealed class RecordingSink : ILogSink
        {
            public List<LogEntry> Entries { get; } =
                new List<LogEntry>();

            public void Write(LogEntry entry)
            {
                Entries.Add(entry);
            }
        }

        private sealed class CallbackSink : ILogSink
        {
            private readonly Action<LogEntry> _callback;

            public CallbackSink(Action<LogEntry> callback)
            {
                _callback = callback;
            }

            public void Write(LogEntry entry)
            {
                _callback(entry);
            }
        }
    }
}
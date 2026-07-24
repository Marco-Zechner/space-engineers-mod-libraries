using System;
using System.Collections.Generic;
using Xunit;

namespace Mz.Logging.Tests
{
    public sealed class CompositeLogSinkTests
    {
        [Fact]
        public void Constructor_NullArray_ThrowsArgumentNullException() 
            => Assert.Throws<ArgumentNullException>(() => new CompositeLogSink(null!));

        [Fact]
        public void Constructor_EmptyArray_ThrowsArgumentException() 
            => Assert.Throws<ArgumentException>(() => new CompositeLogSink());

        [Fact]
        public void Constructor_NullElement_ThrowsArgumentException()
        {
            var sink = new RecordingSink();

            Assert.Throws<ArgumentException>(() => new CompositeLogSink(sink, null!));
        }

        [Fact]
        public void Write_DispatchesSameEntryToEverySink()
        {
            var first = new RecordingSink();
            var second = new RecordingSink();

            var composite = new CompositeLogSink(first, second);

            var entry = CreateEntry();

            composite.Write(entry);

            Assert.Same(entry, Assert.Single(first.Entries));

            Assert.Same(entry, Assert.Single(second.Entries));
        }

        private static readonly string[] Expected1 = ["first", "second", "third"];

        [Fact]
        public void Write_DispatchesInRegistrationOrder()
        {
            var calls = new List<string>();

            var first = new CallbackSink(_ => calls.Add("first"));

            var second = new CallbackSink(_ => calls.Add("second"));

            var third = new CallbackSink(_ => calls.Add("third"));

            var composite = new CompositeLogSink(first, second, third);

            composite.Write(CreateEntry());

            Assert.Equal(Expected1, calls);
        }

        [Fact]
        public void Constructor_CopiesSuppliedSinkArray()
        {
            var original = new RecordingSink();
            var replacement = new RecordingSink();

            ILogSink[] sinks = [original];

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

            Assert.Throws<ArgumentNullException>(() => composite.Write(null!));
        }

        private static readonly string[] Expected2 = ["failing"];

        [Fact]
        public void Write_SinkFailurePropagatesAndStopsDispatch()
        {
            var calls = new List<string>();

            var failing = new CallbackSink(_ =>
            {
                calls.Add("failing");
                throw new InvalidOperationException("Sink failed.");
            });

            var later = new CallbackSink(_ => calls.Add("later"));

            var composite = new CompositeLogSink(failing, later);

            Assert.Throws<InvalidOperationException>(() => composite.Write(CreateEntry()));
            Assert.Equal(Expected2, calls);
        }

        private static LogEntry CreateEntry() 
            => new(DateTime.UtcNow, LogLevel.Information, "CommandAPI", "Example message.", null);

        private sealed class RecordingSink : ILogSink
        {
            public List<LogEntry> Entries { get; } = [];

            public void Write(LogEntry entry) => Entries.Add(entry);
        }

        private sealed class CallbackSink(Action<LogEntry> callback) : ILogSink
        {
            public void Write(LogEntry entry) => callback(entry);
        }
    }
}

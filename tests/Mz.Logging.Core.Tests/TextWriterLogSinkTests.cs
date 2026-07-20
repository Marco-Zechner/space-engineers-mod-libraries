using System;
using System.IO;
using System.Text;
using Xunit;

namespace Mz.Logging.Tests
{
    public sealed class TextWriterLogSinkTests
    {
        [Fact]
        public void Constructor_NullWriter_ThrowsArgumentNullException()
        {
            var formatter = new ConstantFormatter("formatted");

            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    new TextWriterLogSink(
                        null!,
                        formatter
                    );
                }
            );
        }

        [Fact]
        public void Constructor_NullFormatter_ThrowsArgumentNullException()
        {
            var writer = new RecordingTextWriter();

            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    new TextWriterLogSink(
                        writer,
                        null!
                    );
                }
            );
        }

        [Fact]
        public void Write_FormatsAndWritesEntryWithNewline()
        {
            var writer = new RecordingTextWriter();
            var formatter = new ConstantFormatter("formatted entry");

            using (var sink = new TextWriterLogSink(
                writer,
                formatter,
                true
            ))
            {
                var entry = CreateEntry();

                sink.Write(entry);

                Assert.Same(entry, formatter.LastEntry);
                Assert.Equal(
                    "formatted entry\n",
                    writer.Content
                );
            }
        }

        [Fact]
        public void Write_DefaultBehaviorFlushesAfterEveryEntry()
        {
            var writer = new RecordingTextWriter();

            using (var sink = new TextWriterLogSink(
                writer,
                new ConstantFormatter("formatted"),
                true
            ))
            {
                sink.Write(CreateEntry());

                Assert.Equal(1, writer.FlushCount);
            }
        }

        [Fact]
        public void Write_FlushDisabled_DoesNotFlushImmediately()
        {
            var writer = new RecordingTextWriter();

            using (var sink = new TextWriterLogSink(
                writer,
                new ConstantFormatter("formatted"),
                true,
                false
            ))
            {
                sink.Write(CreateEntry());

                Assert.Equal(0, writer.FlushCount);
            }

            Assert.Equal(1, writer.FlushCount);
        }

        [Fact]
        public void Flush_FlushesUnderlyingWriter()
        {
            var writer = new RecordingTextWriter();

            using (var sink = new TextWriterLogSink(
                writer,
                new ConstantFormatter("formatted"),
                true,
                false
            ))
            {
                sink.Flush();

                Assert.Equal(1, writer.FlushCount);
            }
        }

        [Fact]
        public void Dispose_DefaultBehaviorDisposesWriter()
        {
            var writer = new RecordingTextWriter();

            var sink = new TextWriterLogSink(
                writer,
                new ConstantFormatter("formatted")
            );

            sink.Dispose();

            Assert.Equal(1, writer.FlushCount);
            Assert.Equal(1, writer.DisposeCount);
        }

        [Fact]
        public void Dispose_LeaveOpen_DoesNotDisposeWriter()
        {
            var writer = new RecordingTextWriter();

            var sink = new TextWriterLogSink(
                writer,
                new ConstantFormatter("formatted"),
                true
            );

            sink.Dispose();

            Assert.Equal(1, writer.FlushCount);
            Assert.Equal(0, writer.DisposeCount);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DisposesOnlyOnce()
        {
            var writer = new RecordingTextWriter();

            var sink = new TextWriterLogSink(
                writer,
                new ConstantFormatter("formatted")
            );

            sink.Dispose();
            sink.Dispose();

            Assert.Equal(1, writer.FlushCount);
            Assert.Equal(1, writer.DisposeCount);
        }

        [Fact]
        public void Write_NullEntry_ThrowsArgumentNullException()
        {
            var writer = new RecordingTextWriter();

            using (var sink = new TextWriterLogSink(
                writer,
                new ConstantFormatter("formatted"),
                true
            ))
            {
                Assert.Throws<ArgumentNullException>(
                    delegate
                    {
                        sink.Write(null!);
                    }
                );
            }
        }

        [Fact]
        public void Write_AfterDispose_ThrowsInvalidOperationException()
        {
            var sink = new TextWriterLogSink(
                new RecordingTextWriter(),
                new ConstantFormatter("formatted")
            );

            sink.Dispose();

            Assert.Throws<InvalidOperationException>(
                delegate
                {
                    sink.Write(CreateEntry());
                }
            );
        }

        [Fact]
        public void Flush_AfterDispose_ThrowsInvalidOperationException()
        {
            var sink = new TextWriterLogSink(
                new RecordingTextWriter(),
                new ConstantFormatter("formatted")
            );

            sink.Dispose();

            Assert.Throws<InvalidOperationException>(
                delegate
                {
                    sink.Flush();
                }
            );
        }

        private static LogEntry CreateEntry()
        {
            return new LogEntry(
                DateTime.UtcNow,
                LogLevel.Information,
                "CommandAPI",
                "Example message.",
                null
            );
        }

        private sealed class ConstantFormatter : ILogFormatter
        {
            private readonly string _result;

            public LogEntry? LastEntry { get; private set; }

            public ConstantFormatter(string result)
            {
                _result = result;
            }

            public string Format(LogEntry entry)
            {
                LastEntry = entry;
                return _result;
            }
        }

        private sealed class RecordingTextWriter : TextWriter
        {
            private readonly StringBuilder _content =
                new StringBuilder();

            public override Encoding Encoding
            {
                get
                {
                    return Encoding.UTF8;
                }
            }

            public string Content
            {
                get
                {
                    return _content.ToString();
                }
            }

            public int FlushCount { get; private set; }

            public int DisposeCount { get; private set; }

            public RecordingTextWriter()
            {
                NewLine = "\n";
            }

            public override void Write(char value)
            {
                _content.Append(value);
            }

            public override void Write(string? value)
            {
                _content.Append(value);
            }

            public override void Flush()
            {
                FlushCount++;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    DisposeCount++;

                base.Dispose(disposing);
            }
        }
    }
}

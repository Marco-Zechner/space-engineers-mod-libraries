using System;
using System.IO;
using System.Text;
using Mz.Logging;
using Xunit;

namespace Mz.Logging.SpaceEngineers.Tests
{
    public sealed class SpaceEngineersStorageLoggerTests
    {
        [Fact]
        public void CreateLocal_OpensLocalStorageWithCallingType()
        {
            var factory = new RecordingStorageWriterFactory();

            using (var logging = SpaceEngineersStorageLogger.CreateLocal(
                factory,
                new ConstantFormatter("formatted"),
                "  CommandAPI.log  ",
                typeof(SpaceEngineersStorageLoggerTests),
                "CommandAPI",
                LogLevel.Information
            ))
            {
                Assert.Equal(StorageCall.Local, factory.LastCall);
                Assert.Equal("CommandAPI.log", factory.LastFileName);

                Assert.Equal(
                    typeof(SpaceEngineersStorageLoggerTests),
                    factory.LastCallingType
                );

                Assert.Equal(1, factory.OpenCount);
            }
        }

        [Fact]
        public void CreateWorld_OpensWorldStorageWithCallingType()
        {
            var factory = new RecordingStorageWriterFactory();

            using (var logging = SpaceEngineersStorageLogger.CreateWorld(
                factory,
                new ConstantFormatter("formatted"),
                "  CommandAPI.log  ",
                typeof(SpaceEngineersStorageLoggerTests),
                "CommandAPI",
                LogLevel.Information
            ))
            {
                Assert.Equal(StorageCall.World, factory.LastCall);
                Assert.Equal("CommandAPI.log", factory.LastFileName);

                Assert.Equal(
                    typeof(SpaceEngineersStorageLoggerTests),
                    factory.LastCallingType
                );

                Assert.Equal(1, factory.OpenCount);
            }
        }

        [Fact]
        public void CreateGlobalUnsafe_OpensGlobalStorageWithoutCallingType()
        {
            var factory = new RecordingStorageWriterFactory();

            using (var logging =
                   SpaceEngineersStorageLogger.CreateGlobalUnsafe(
                       factory,
                       new ConstantFormatter("formatted"),
                       "  Mz.CommandAPI.log  ",
                       "CommandAPI",
                       LogLevel.Information
                   ))
            {
                Assert.Equal(StorageCall.Global, factory.LastCall);
                Assert.Equal(
                    "Mz.CommandAPI.log",
                    factory.LastFileName
                );

                Assert.Null(factory.LastCallingType);
                Assert.Equal(1, factory.OpenCount);
            }
        }

        [Fact]
        public void Logger_WritesThroughOpenedWriter()
        {
            var factory = new RecordingStorageWriterFactory();

            using (var logging = SpaceEngineersStorageLogger.CreateLocal(
                factory,
                new ConstantFormatter("formatted entry"),
                "CommandAPI.log",
                typeof(SpaceEngineersStorageLoggerTests),
                "CommandAPI",
                LogLevel.Information
            ))
            {
                logging.Logger.Info("Original message.");

                Assert.Equal(
                    "formatted entry\n",
                    factory.Writer.Content
                );
            }
        }

        [Fact]
        public void Logger_FlushesEveryWrittenEntry()
        {
            var factory = new RecordingStorageWriterFactory();

            using (var logging = SpaceEngineersStorageLogger.CreateWorld(
                factory,
                new ConstantFormatter("formatted"),
                "CommandAPI.log",
                typeof(SpaceEngineersStorageLoggerTests),
                "CommandAPI",
                LogLevel.Information
            ))
            {
                logging.Logger.Info("Message.");

                Assert.Equal(1, factory.Writer.FlushCount);
            }
        }

        [Fact]
        public void Logger_UsesConfiguredSourceAndMinimumLevel()
        {
            var factory = new RecordingStorageWriterFactory();

            using (var logging =
                   SpaceEngineersStorageLogger.CreateGlobalUnsafe(
                       factory,
                       new ConstantFormatter("formatted"),
                       "Mz.CommandAPI.log",
                       "  CommandAPI  ",
                       LogLevel.Warning
                   ))
            {
                Assert.Equal("CommandAPI", logging.Logger.Source);

                Assert.Equal(
                    LogLevel.Warning,
                    logging.Logger.MinimumLevel
                );
            }
        }

        [Fact]
        public void Flush_FlushesOwnedWriter()
        {
            var factory = new RecordingStorageWriterFactory();

            using (var logging = SpaceEngineersStorageLogger.CreateLocal(
                factory,
                new ConstantFormatter("formatted"),
                "CommandAPI.log",
                typeof(SpaceEngineersStorageLoggerTests),
                "CommandAPI",
                LogLevel.Information
            ))
            {
                logging.Flush();

                Assert.Equal(1, factory.Writer.FlushCount);
            }
        }

        [Fact]
        public void Dispose_FlushesAndDisposesOwnedWriter()
        {
            var factory = new RecordingStorageWriterFactory();

            var logging = SpaceEngineersStorageLogger.CreateLocal(
                factory,
                new ConstantFormatter("formatted"),
                "CommandAPI.log",
                typeof(SpaceEngineersStorageLoggerTests),
                "CommandAPI",
                LogLevel.Information
            );

            logging.Dispose();

            Assert.Equal(1, factory.Writer.FlushCount);
            Assert.Equal(1, factory.Writer.DisposeCount);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DisposesOnlyOnce()
        {
            var factory = new RecordingStorageWriterFactory();

            var logging = SpaceEngineersStorageLogger.CreateWorld(
                factory,
                new ConstantFormatter("formatted"),
                "CommandAPI.log",
                typeof(SpaceEngineersStorageLoggerTests),
                "CommandAPI",
                LogLevel.Information
            );

            logging.Dispose();
            logging.Dispose();

            Assert.Equal(1, factory.Writer.FlushCount);
            Assert.Equal(1, factory.Writer.DisposeCount);
        }

        [Fact]
        public void Flush_AfterDispose_ThrowsInvalidOperationException()
        {
            var factory = new RecordingStorageWriterFactory();

            var logging =
                SpaceEngineersStorageLogger.CreateGlobalUnsafe(
                    factory,
                    new ConstantFormatter("formatted"),
                    "Mz.CommandAPI.log",
                    "CommandAPI",
                    LogLevel.Information
                );

            logging.Dispose();

            Assert.Throws<InvalidOperationException>(
                delegate
                {
                    logging.Flush();
                }
            );
        }

        [Fact]
        public void LoggerWrite_AfterDispose_ThrowsInvalidOperationException()
        {
            var factory = new RecordingStorageWriterFactory();

            var logging = SpaceEngineersStorageLogger.CreateLocal(
                factory,
                new ConstantFormatter("formatted"),
                "CommandAPI.log",
                typeof(SpaceEngineersStorageLoggerTests),
                "CommandAPI",
                LogLevel.Information
            );

            logging.Dispose();

            Assert.Throws<InvalidOperationException>(
                delegate
                {
                    logging.Logger.Info("Too late.");
                }
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateLocal_InvalidFileName_DoesNotOpenWriter(
            string? fileName
        )
        {
            var factory = new RecordingStorageWriterFactory();

            Assert.ThrowsAny<ArgumentException>(
                delegate
                {
                    SpaceEngineersStorageLogger.CreateLocal(
                        factory,
                        new ConstantFormatter("formatted"),
                        fileName!,
                        typeof(SpaceEngineersStorageLoggerTests),
                        "CommandAPI",
                        LogLevel.Information
                    );
                }
            );

            Assert.Equal(0, factory.OpenCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateWorld_InvalidFileName_DoesNotOpenWriter(
            string? fileName
        )
        {
            var factory = new RecordingStorageWriterFactory();

            Assert.ThrowsAny<ArgumentException>(
                delegate
                {
                    SpaceEngineersStorageLogger.CreateWorld(
                        factory,
                        new ConstantFormatter("formatted"),
                        fileName!,
                        typeof(SpaceEngineersStorageLoggerTests),
                        "CommandAPI",
                        LogLevel.Information
                    );
                }
            );

            Assert.Equal(0, factory.OpenCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateGlobalUnsafe_InvalidFileName_DoesNotOpenWriter(
            string? fileName
        )
        {
            var factory = new RecordingStorageWriterFactory();

            Assert.ThrowsAny<ArgumentException>(
                delegate
                {
                    SpaceEngineersStorageLogger.CreateGlobalUnsafe(
                        factory,
                        new ConstantFormatter("formatted"),
                        fileName!,
                        "CommandAPI",
                        LogLevel.Information
                    );
                }
            );

            Assert.Equal(0, factory.OpenCount);
        }

        [Fact]
        public void CreateLocal_NullCallingType_DoesNotOpenWriter()
        {
            var factory = new RecordingStorageWriterFactory();

            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    SpaceEngineersStorageLogger.CreateLocal(
                        factory,
                        new ConstantFormatter("formatted"),
                        "CommandAPI.log",
                        null!,
                        "CommandAPI",
                        LogLevel.Information
                    );
                }
            );

            Assert.Equal(0, factory.OpenCount);
        }

        [Fact]
        public void CreateWorld_NullCallingType_DoesNotOpenWriter()
        {
            var factory = new RecordingStorageWriterFactory();

            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    SpaceEngineersStorageLogger.CreateWorld(
                        factory,
                        new ConstantFormatter("formatted"),
                        "CommandAPI.log",
                        null!,
                        "CommandAPI",
                        LogLevel.Information
                    );
                }
            );

            Assert.Equal(0, factory.OpenCount);
        }

        [Fact]
        public void CreateLocal_NullFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    SpaceEngineersStorageLogger.CreateLocal(
                        null!,
                        new ConstantFormatter("formatted"),
                        "CommandAPI.log",
                        typeof(SpaceEngineersStorageLoggerTests),
                        "CommandAPI",
                        LogLevel.Information
                    );
                }
            );
        }

        [Fact]
        public void CreateWorld_NullFormatter_DoesNotOpenWriter()
        {
            var factory = new RecordingStorageWriterFactory();

            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    SpaceEngineersStorageLogger.CreateWorld(
                        factory,
                        null!,
                        "CommandAPI.log",
                        typeof(SpaceEngineersStorageLoggerTests),
                        "CommandAPI",
                        LogLevel.Information
                    );
                }
            );

            Assert.Equal(0, factory.OpenCount);
        }

        [Fact]
        public void CreateGlobalUnsafe_NullWriterFromFactory_Throws()
        {
            var factory = new NullStorageWriterFactory();

            Assert.Throws<InvalidOperationException>(
                delegate
                {
                    SpaceEngineersStorageLogger.CreateGlobalUnsafe(
                        factory,
                        new ConstantFormatter("formatted"),
                        "Mz.CommandAPI.log",
                        "CommandAPI",
                        LogLevel.Information
                    );
                }
            );
        }

        [Fact]
        public void CreateLocal_InvalidLoggerConfiguration_DisposesWriter()
        {
            var factory = new RecordingStorageWriterFactory();

            Assert.Throws<ArgumentException>(
                delegate
                {
                    SpaceEngineersStorageLogger.CreateLocal(
                        factory,
                        new ConstantFormatter("formatted"),
                        "CommandAPI.log",
                        typeof(SpaceEngineersStorageLoggerTests),
                        "   ",
                        LogLevel.Information
                    );
                }
            );

            Assert.Equal(1, factory.OpenCount);
            Assert.Equal(1, factory.Writer.DisposeCount);
        }

        [Fact]
        public void CreateWorld_InvalidMinimumLevel_DisposesWriter()
        {
            var factory = new RecordingStorageWriterFactory();

            Assert.Throws<ArgumentException>(
                delegate
                {
                    SpaceEngineersStorageLogger.CreateWorld(
                        factory,
                        new ConstantFormatter("formatted"),
                        "CommandAPI.log",
                        typeof(SpaceEngineersStorageLoggerTests),
                        "CommandAPI",
                        (LogLevel)100
                    );
                }
            );

            Assert.Equal(1, factory.OpenCount);
            Assert.Equal(1, factory.Writer.DisposeCount);
        }

        private enum StorageCall
        {
            None,
            Local,
            World,
            Global
        }

        private sealed class RecordingStorageWriterFactory :
            IStorageWriterFactory
        {
            public RecordingTextWriter Writer { get; } =
                new RecordingTextWriter();

            public StorageCall LastCall { get; private set; }

            public string? LastFileName { get; private set; }

            public Type? LastCallingType { get; private set; }

            public int OpenCount { get; private set; }

            public TextWriter OpenLocal(
                string fileName,
                Type callingType
            )
            {
                Record(
                    StorageCall.Local,
                    fileName,
                    callingType
                );

                return Writer;
            }

            public TextWriter OpenWorld(
                string fileName,
                Type callingType
            )
            {
                Record(
                    StorageCall.World,
                    fileName,
                    callingType
                );

                return Writer;
            }

            public TextWriter OpenGlobal(string fileName)
            {
                Record(
                    StorageCall.Global,
                    fileName,
                    null
                );

                return Writer;
            }

            private void Record(
                StorageCall call,
                string fileName,
                Type? callingType
            )
            {
                OpenCount++;
                LastCall = call;
                LastFileName = fileName;
                LastCallingType = callingType;
            }
        }

        private sealed class NullStorageWriterFactory :
            IStorageWriterFactory
        {
            public TextWriter OpenLocal(
                string fileName,
                Type callingType
            )
            {
                return null!;
            }

            public TextWriter OpenWorld(
                string fileName,
                Type callingType
            )
            {
                return null!;
            }

            public TextWriter OpenGlobal(string fileName)
            {
                return null!;
            }
        }

        private sealed class ConstantFormatter : ILogFormatter
        {
            private readonly string _formatted;

            public ConstantFormatter(string formatted)
            {
                _formatted = formatted;
            }

            public string Format(LogEntry entry)
            {
                return _formatted;
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

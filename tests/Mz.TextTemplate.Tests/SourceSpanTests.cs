using System;
using Xunit;

namespace Mz.TextTemplate.Tests
{
    public sealed class SourceSpanTests
    {
        [Fact]
        public void Constructor_StoresHalfOpenRange()
        {
            var span =
                new SourceSpan(4, 3);

            Assert.Equal(4, span.Start);
            Assert.Equal(3, span.Length);
            Assert.Equal(7, span.End);

            Assert.True(span.Contains(4));
            Assert.True(span.Contains(6));
            Assert.False(span.Contains(7));

            Assert.Equal(
                "[4..7)",
                span.ToString()
            );
        }

        [Theory]
        [InlineData(-1, 0, "start")]
        [InlineData(0, -1, "length")]
        public void Constructor_NegativeValue_ThrowsArgumentException(
            int start,
            int length,
            string parameter
        )
        {
            var exception =
                Assert.Throws<ArgumentException>(
                    () =>
                        new SourceSpan(
                            start,
                            length
                        )
                );

            Assert.Equal(
                parameter,
                exception.ParamName
            );
        }

        [Fact]
        public void Constructor_OverflowingEnd_ThrowsArgumentException()
        {
            var exception =
                Assert.Throws<ArgumentException>(
                    () =>
                        new SourceSpan(
                            int.MaxValue,
                            1
                        )
                );

            Assert.Equal(
                "length",
                exception.ParamName
            );
        }

        [Fact]
        public void Equality_UsesStartAndLength()
        {
            Assert.Equal(
                new SourceSpan(1, 2),
                new SourceSpan(1, 2)
            );

            Assert.NotEqual(
                new SourceSpan(1, 2),
                new SourceSpan(1, 3)
            );
        }
    }
}
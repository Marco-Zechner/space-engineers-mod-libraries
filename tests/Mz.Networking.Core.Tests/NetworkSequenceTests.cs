using System;
using Xunit;

namespace Mz.Networking.Tests
{
    public sealed class NetworkSequenceTests
    {
        [Theory]
        [InlineData(1, 0, true)]
        [InlineData(32767, 0, true)]
        [InlineData(0, 65535, true)]
        [InlineData(0, 0, false)]
        [InlineData(9, 10, false)]
        [InlineData(32768, 0, false)]
        [InlineData(0, 32768, false)]
        public void IsNewer_UsesWrappingHalfRange(
            int candidate,
            int current,
            bool expected)
        {
            Assert.Equal(
                expected,
                NetworkSequence.IsNewer(
                    (ushort)candidate,
                    (ushort)current
                )
            );
        }

        [Fact]
        public void Tracker_FirstSequenceIsAccepted()
        {
            var tracker = new NetworkSequenceTracker();

            Assert.True(tracker.TryAccept(500));
            Assert.True(tracker.HasSequence);
            Assert.Equal((ushort)500, tracker.LatestSequence);
        }

        [Fact]
        public void Tracker_DuplicateStaleAndAmbiguousSequencesAreRejected()
        {
            var tracker = new NetworkSequenceTracker();

            Assert.True(tracker.TryAccept(100));
            Assert.False(tracker.TryAccept(100));
            Assert.False(tracker.TryAccept(99));
            Assert.False(tracker.TryAccept(32868));
            Assert.Equal((ushort)100, tracker.LatestSequence);
        }

        [Fact]
        public void Tracker_WraparoundSequenceIsAccepted()
        {
            var tracker = new NetworkSequenceTracker();

            Assert.True(tracker.TryAccept(ushort.MaxValue));
            Assert.True(tracker.TryAccept(0));
            Assert.Equal((ushort)0, tracker.LatestSequence);
        }

        [Fact]
        public void Tracker_ResetStartsANewStream()
        {
            var tracker = new NetworkSequenceTracker();

            Assert.True(tracker.TryAccept(500));
            tracker.Reset();

            Assert.False(tracker.HasSequence);

            Assert.Throws<InvalidOperationException>(
                delegate
                {
                    ushort ignored = tracker.LatestSequence;
                }
            );

            Assert.True(tracker.TryAccept(100));
            Assert.Equal((ushort)100, tracker.LatestSequence);
        }
    }
}

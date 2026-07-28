using System;

namespace Mz.Networking
{
    /// <summary>
    /// Tracks the latest accepted wrapping unsigned 16-bit sequence for one
    /// application stream.
    /// </summary>
    public sealed class NetworkSequenceTracker
    {
        private bool _hasSequence;
        private ushort _latestSequence;

        /// <summary>
        /// Gets whether a sequence has been accepted.
        /// </summary>
        public bool HasSequence => _hasSequence;

        /// <summary>
        /// Gets the latest accepted sequence.
        /// </summary>
        public ushort LatestSequence
        {
            get
            {
                if (!_hasSequence)
                    throw new InvalidOperationException("No network sequence has been accepted.");

                return _latestSequence;
            }
        }

        /// <summary>
        /// Accepts the first sequence and later sequences that are newer than
        /// the latest accepted value.
        /// </summary>
        public bool TryAccept(ushort sequence)
        {
            if (_hasSequence && !NetworkSequence.IsNewer(sequence, _latestSequence))
                return false;

            _latestSequence = sequence;
            _hasSequence = true;
            return true;
        }

        /// <summary>
        /// Clears the latest sequence when its application stream is recreated.
        /// </summary>
        public void Reset()
        {
            _hasSequence = false;
            _latestSequence = 0;
        }
    }
}

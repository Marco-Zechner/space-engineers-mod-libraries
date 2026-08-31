using System;

namespace Mz.TextTemplate
{
    /// <summary>
    /// Identifies a half-open range of characters in template source text.
    /// </summary>
    public struct SourceSpan : IEquatable<SourceSpan>
    {
        /// <summary>
        /// Creates a source span beginning at <paramref name="start"/> and
        /// containing <paramref name="length"/> characters.
        /// </summary>
        public SourceSpan(int start, int length)
        {
            if (start < 0)
                throw new ArgumentException("Source span start cannot be negative.", nameof(start));

            if (length < 0)
                throw new ArgumentException("Source span length cannot be negative.", nameof(length));

            if (length > int.MaxValue - start)
                throw new ArgumentException("Source span end cannot exceed Int32.MaxValue.", nameof(length));

            Start = start;
            Length = length;
        }

        /// <summary>
        /// Gets the zero-based source offset at which the span begins.
        /// </summary>
        public int Start { get; private set; }

        /// <summary>
        /// Gets the number of characters contained by the span.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Gets the exclusive zero-based source offset at which the span ends.
        /// </summary>
        public int End
        {
            get { return Start + Length; }
        }

        /// <summary>
        /// Returns whether the supplied source offset lies inside this span.
        /// </summary>
        public bool Contains(int offset)
        {
            return offset >= Start && offset < End;
        }

        /// <summary>
        /// Returns whether another source span has the same start and length.
        /// </summary>
        public bool Equals(SourceSpan other)
        {
            return Start == other.Start && Length == other.Length;
        }

        /// <summary>
        /// Returns whether another object is an equal source span.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is SourceSpan && Equals((SourceSpan)obj);
        }

        /// <summary>
        /// Returns a hash code derived from the span start and length.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Start * 397) ^ Length;
            }
        }

        /// <summary>
        /// Returns the span using half-open range notation.
        /// </summary>
        public override string ToString()
        {
            return "[" + Start + ".." + End + ")";
        }

        /// <summary>
        /// Returns whether two source spans are equal.
        /// </summary>
        public static bool operator ==(SourceSpan left, SourceSpan right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns whether two source spans are different.
        /// </summary>
        public static bool operator !=(SourceSpan left, SourceSpan right)
        {
            return !left.Equals(right);
        }
    }
}

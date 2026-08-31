using System;
using System.Globalization;

namespace Mz.Toml
{
    /// <summary>
    /// Represents a TOML local time without a date or timezone.
    /// </summary>
    public sealed class TomlLocalTime
    {
        private readonly int _hour;
        private readonly int _minute;
        private readonly int _second;
        private readonly string _fractionalSeconds;

        /// <summary>
        /// Initializes a TOML local time without fractional seconds.
        /// </summary>
        public TomlLocalTime(int hour, int minute, int second)
            : this(hour, minute, second, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a TOML local time.
        /// Fractional seconds are supplied as decimal digits without a dot.
        /// </summary>
        public TomlLocalTime(int hour, int minute, int second, string fractionalSeconds)
        {
            if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 60)
            {
                throw new ArgumentException(
                    "The supplied components do not form a valid TOML local time.");
            }

            if (fractionalSeconds == null)
                throw new ArgumentNullException(nameof(fractionalSeconds));

            for (var i = 0; i < fractionalSeconds.Length; i++)
            {
                var c = fractionalSeconds[i];

                if (c < '0' || c > '9')
                {
                    throw new ArgumentException(
                        "TOML fractional seconds must contain only decimal digits.");
                }
            }

            _hour = hour;
            _minute = minute;
            _second = second;
            _fractionalSeconds = fractionalSeconds;
        }

        /// <summary>
        /// Gets the hour from 0 through 23.
        /// </summary>
        public int Hour => _hour;

        /// <summary>
        /// Gets the minute from 0 through 59.
        /// </summary>
        public int Minute => _minute;

        /// <summary>
        /// Gets the second. TOML permits the leap-second spelling 60.
        /// </summary>
        public int Second => _second;

        /// <summary>
        /// Gets fractional-second decimal digits without the leading dot.
        /// An empty string means that no fraction was supplied.
        /// </summary>
        public string FractionalSeconds => _fractionalSeconds;

        /// <summary>
        /// Returns the canonical TOML local-time spelling.
        /// </summary>
        public override string ToString()
        {
            var text = _hour.ToString("D2", CultureInfo.InvariantCulture) + ":" +
                       _minute.ToString("D2", CultureInfo.InvariantCulture) + ":" +
                       _second.ToString("D2", CultureInfo.InvariantCulture);

            if (_fractionalSeconds.Length > 0)
                text += "." + _fractionalSeconds;

            return text;
        }
    }
}

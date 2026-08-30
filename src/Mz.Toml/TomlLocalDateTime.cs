using System;

namespace Mz.Toml
{
    /// <summary>
    /// Represents a TOML local date-time without a timezone.
    /// </summary>
    public sealed class TomlLocalDateTime
    {
        private readonly TomlLocalDate _date;
        private readonly TomlLocalTime _time;

        /// <summary>
        /// Initializes a TOML local date-time.
        /// </summary>
        public TomlLocalDateTime(
            TomlLocalDate date,
            TomlLocalTime time)
        {
            if (date == null)
                throw new ArgumentNullException("date");

            if (time == null)
                throw new ArgumentNullException("time");

            _date = date;
            _time = time;
        }

        /// <summary>
        /// Initializes a TOML local date-time from components.
        /// Fractional seconds are supplied as decimal digits without a dot.
        /// </summary>
        public TomlLocalDateTime(
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int second,
            string fractionalSeconds)
            : this(
                new TomlLocalDate(
                    year,
                    month,
                    day),
                new TomlLocalTime(
                    hour,
                    minute,
                    second,
                    fractionalSeconds))
        {
        }

        /// <summary>
        /// Gets the local date component.
        /// </summary>
        public TomlLocalDate Date
        {
            get { return _date; }
        }

        /// <summary>
        /// Gets the local time component.
        /// </summary>
        public TomlLocalTime Time
        {
            get { return _time; }
        }

        /// <summary>
        /// Returns the canonical TOML local-date-time spelling.
        /// </summary>
        public override string ToString()
        {
            return
                _date.ToString() +
                "T" +
                _time.ToString();
        }
    }
}
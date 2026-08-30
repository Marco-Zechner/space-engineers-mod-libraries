using System;
using System.Globalization;

namespace Mz.Toml
{
    /// <summary>
    /// Represents a TOML offset date-time.
    /// </summary>
    public sealed class TomlOffsetDateTime
    {
        private readonly TomlLocalDate _date;
        private readonly TomlLocalTime _time;
        private readonly int _offsetMinutes;
        private readonly bool _isUnknownLocalOffset;

        /// <summary>
        /// Initializes a TOML offset date-time with a known UTC offset.
        /// </summary>
        public TomlOffsetDateTime(
            TomlLocalDate date,
            TomlLocalTime time,
            int offsetMinutes)
            : this(
                date,
                time,
                offsetMinutes,
                false)
        {
        }

        /// <summary>
        /// Initializes a TOML offset date-time.
        /// Set <paramref name="isUnknownLocalOffset"/> only for the RFC 3339
        /// negative-zero offset spelling -00:00.
        /// </summary>
        public TomlOffsetDateTime(
            TomlLocalDate date,
            TomlLocalTime time,
            int offsetMinutes,
            bool isUnknownLocalOffset)
        {
            if (date == null)
                throw new ArgumentNullException("date");

            if (time == null)
                throw new ArgumentNullException("time");

            if (offsetMinutes < -1439 ||
                offsetMinutes > 1439)
            {
                throw new ArgumentException(
                    "TOML numeric UTC offsets must be between -23:59 and +23:59.");
            }

            if (isUnknownLocalOffset &&
                offsetMinutes != 0)
            {
                throw new ArgumentException(
                    "The RFC 3339 unknown-local-offset marker is only valid with offset 00:00.");
            }

            _date = date;
            _time = time;
            _offsetMinutes = offsetMinutes;
            _isUnknownLocalOffset =
                isUnknownLocalOffset;
        }

        /// <summary>
        /// Initializes a TOML offset date-time from components.
        /// Fractional seconds are supplied as decimal digits without a dot.
        /// </summary>
        public TomlOffsetDateTime(
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int second,
            string fractionalSeconds,
            int offsetMinutes)
            : this(
                new TomlLocalDate(
                    year,
                    month,
                    day),
                new TomlLocalTime(
                    hour,
                    minute,
                    second,
                    fractionalSeconds),
                offsetMinutes,
                false)
        {
        }

        /// <summary>
        /// Initializes a TOML offset date-time from components while
        /// preserving the RFC 3339 -00:00 unknown-local-offset marker.
        /// </summary>
        public TomlOffsetDateTime(
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int second,
            string fractionalSeconds,
            int offsetMinutes,
            bool isUnknownLocalOffset)
            : this(
                new TomlLocalDate(
                    year,
                    month,
                    day),
                new TomlLocalTime(
                    hour,
                    minute,
                    second,
                    fractionalSeconds),
                offsetMinutes,
                isUnknownLocalOffset)
        {
        }

        /// <summary>
        /// Gets the local date component before applying the offset.
        /// </summary>
        public TomlLocalDate Date
        {
            get { return _date; }
        }

        /// <summary>
        /// Gets the local time component before applying the offset.
        /// </summary>
        public TomlLocalTime Time
        {
            get { return _time; }
        }

        /// <summary>
        /// Gets the signed UTC offset in minutes.
        /// </summary>
        public int OffsetMinutes
        {
            get { return _offsetMinutes; }
        }

        /// <summary>
        /// Gets whether the value used RFC 3339's -00:00 marker indicating
        /// that the local UTC offset is unknown.
        /// </summary>
        public bool IsUnknownLocalOffset
        {
            get { return _isUnknownLocalOffset; }
        }

        /// <summary>
        /// Returns a deterministic TOML offset-date-time spelling.
        /// Known zero offset is written as Z and unknown zero offset as
        /// -00:00.
        /// </summary>
        public override string ToString()
        {
            var text =
                _date.ToString() +
                "T" +
                _time.ToString();

            if (_isUnknownLocalOffset)
                return text + "-00:00";

            if (_offsetMinutes == 0)
                return text + "Z";

            var absolute =
                _offsetMinutes < 0
                    ? -_offsetMinutes
                    : _offsetMinutes;

            var offsetHour =
                absolute / 60;

            var offsetMinute =
                absolute % 60;

            return
                text +
                (_offsetMinutes < 0
                    ? "-"
                    : "+") +
                offsetHour.ToString(
                    "D2",
                    CultureInfo.InvariantCulture) +
                ":" +
                offsetMinute.ToString(
                    "D2",
                    CultureInfo.InvariantCulture);
        }
    }
}
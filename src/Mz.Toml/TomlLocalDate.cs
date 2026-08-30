using System;
using System.Globalization;

namespace Mz.Toml
{
    /// <summary>
    /// Represents a TOML local date without a timezone or time of day.
    /// </summary>
    public sealed class TomlLocalDate
    {
        private readonly int _year;
        private readonly int _month;
        private readonly int _day;

        /// <summary>
        /// Initializes a TOML local date.
        /// </summary>
        public TomlLocalDate(
            int year,
            int month,
            int day)
        {
            if (!IsValidDate(
                    year,
                    month,
                    day))
            {
                throw new ArgumentException(
                    "The supplied components do not form a valid TOML local date.");
            }

            _year = year;
            _month = month;
            _day = day;
        }

        /// <summary>
        /// Gets the four-digit year from 0000 through 9999.
        /// </summary>
        public int Year
        {
            get { return _year; }
        }

        /// <summary>
        /// Gets the month from 1 through 12.
        /// </summary>
        public int Month
        {
            get { return _month; }
        }

        /// <summary>
        /// Gets the day of month.
        /// </summary>
        public int Day
        {
            get { return _day; }
        }

        /// <summary>
        /// Returns the canonical TOML local-date spelling.
        /// </summary>
        public override string ToString()
        {
            return
                _year.ToString(
                    "D4",
                    CultureInfo.InvariantCulture) +
                "-" +
                _month.ToString(
                    "D2",
                    CultureInfo.InvariantCulture) +
                "-" +
                _day.ToString(
                    "D2",
                    CultureInfo.InvariantCulture);
        }

        internal static bool IsValidDate(
            int year,
            int month,
            int day)
        {
            if (year < 0 ||
                year > 9999 ||
                month < 1 ||
                month > 12 ||
                day < 1)
            {
                return false;
            }

            var days = DaysInMonth(
                year,
                month);

            return day <= days;
        }

        private static int DaysInMonth(
            int year,
            int month)
        {
            switch (month)
            {
                case 2:
                    return IsLeapYear(year)
                        ? 29
                        : 28;

                case 4:
                case 6:
                case 9:
                case 11:
                    return 30;

                default:
                    return 31;
            }
        }

        private static bool IsLeapYear(
            int year)
        {
            if ((year % 400) == 0)
                return true;

            if ((year % 100) == 0)
                return false;

            return (year % 4) == 0;
        }
    }
}
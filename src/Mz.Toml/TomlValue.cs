using System;

namespace Mz.Toml
{
    /// <summary>
    /// Represents a scalar TOML value.
    /// </summary>
    public sealed class TomlValue : TomlNode
    {
        private readonly TomlValueKind _valueKind;
        private readonly object _value;

        internal TomlValue(
            TomlValueKind valueKind,
            object value,
            int line,
            int column)
            : base(TomlNodeKind.Value, line, column)
        {
            _valueKind = valueKind;
            _value = value;
        }

        /// <summary>
        /// Gets the scalar value kind.
        /// </summary>
        public TomlValueKind ValueKind
        {
            get { return _valueKind; }
        }

        /// <summary>
        /// Creates a TOML string value.
        /// </summary>
        public static TomlValue FromString(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return new TomlValue(TomlValueKind.String, value, 0, 0);
        }

        /// <summary>
        /// Creates a TOML integer value.
        /// </summary>
        public static TomlValue FromInteger(long value)
        {
            return new TomlValue(TomlValueKind.Integer, value, 0, 0);
        }

        /// <summary>
        /// Creates a TOML floating-point value.
        /// </summary>
        public static TomlValue FromFloat(double value)
        {
            return new TomlValue(TomlValueKind.Float, value, 0, 0);
        }

        /// <summary>
        /// Creates a TOML Boolean value.
        /// </summary>
        public static TomlValue FromBoolean(bool value)
        {
            return new TomlValue(TomlValueKind.Boolean, value, 0, 0);
        }

        /// <summary>
        /// Returns the value as a string.
        /// </summary>
        public string AsString()
        {
            RequireKind(TomlValueKind.String);
            return (string)_value;
        }

        /// <summary>
        /// Returns the value as a 64-bit integer.
        /// </summary>
        public long AsInteger()
        {
            RequireKind(TomlValueKind.Integer);
            return (long)_value;
        }

        /// <summary>
        /// Returns the value as a double-precision floating-point number.
        /// </summary>
        public double AsFloat()
        {
            RequireKind(TomlValueKind.Float);
            return (double)_value;
        }

        /// <summary>
        /// Returns the value as a Boolean.
        /// </summary>
        public bool AsBoolean()
        {
            RequireKind(TomlValueKind.Boolean);
            return (bool)_value;
        }

        private void RequireKind(TomlValueKind expected)
        {
            if (_valueKind != expected)
            {
                throw new InvalidOperationException(
                    "TOML value is " + _valueKind + ", not " + expected + ".");
            }
        }
    }
}
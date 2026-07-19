using System;
using System.Globalization;

namespace Mz.SemanticVersioning
{
    /// <summary>
    /// Represents a semantic version containing major, minor, and patch
    /// components.
    /// </summary>
    public sealed class SemanticVersion
    {
        /// <summary>
        /// Gets the major version component.
        /// </summary>
        public int Major { get; private set; }

        /// <summary>
        /// Gets the minor version component.
        /// </summary>
        public int Minor { get; private set; }

        /// <summary>
        /// Gets the patch version component.
        /// </summary>
        public int Patch { get; private set; }

        /// <summary>
        /// Creates a semantic version from its numeric components.
        /// </summary>
        /// <param name="major">The non-negative major component.</param>
        /// <param name="minor">The non-negative minor component.</param>
        /// <param name="patch">The non-negative patch component.</param>
        public SemanticVersion(int major, int minor, int patch)
        {
            if (major < 0)
                throw new ArgumentOutOfRangeException(nameof(major));

            if (minor < 0)
                throw new ArgumentOutOfRangeException(nameof(minor));

            if (patch < 0)
                throw new ArgumentOutOfRangeException(nameof(patch));

            Major = major;
            Minor = minor;
            Patch = patch;
        }

        /// <summary>
        /// Parses an exact major.minor.patch version string.
        /// </summary>
        /// <param name="value">The version string to parse.</param>
        /// <returns>The parsed semantic version.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="value"/> is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown when the value is not a valid major.minor.patch version.
        /// </exception>
        public static SemanticVersion Parse(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            SemanticVersion version;

            if (!TryParse(value, out version))
            {
                throw new FormatException(
                    "Semantic version must use the format major.minor.patch " +
                    "with three non-negative decimal integer components."
                );
            }

            return version;
        }

        /// <summary>
        /// Attempts to parse an exact major.minor.patch version string.
        /// </summary>
        /// <param name="value">The version string to parse.</param>
        /// <param name="version">
        /// Receives the parsed version, or null when parsing fails.
        /// </param>
        /// <returns>True when parsing succeeds; otherwise false.</returns>
        public static bool TryParse(
            string value,
            out SemanticVersion version
        )
        {
            version = null;

            if (value == null)
                return false;

            string trimmedValue = value.Trim();

            if (trimmedValue.Length == 0)
                return false;

            string[] components = trimmedValue.Split('.');

            if (components.Length != 3)
                return false;

            int major;
            int minor;
            int patch;

            if (!TryParseComponent(components[0], out major))
                return false;

            if (!TryParseComponent(components[1], out minor))
                return false;

            if (!TryParseComponent(components[2], out patch))
                return false;

            version = new SemanticVersion(major, minor, patch);
            return true;
        }

        /// <summary>
        /// Returns the normalized major.minor.patch representation.
        /// </summary>
        /// <returns>The normalized version string.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}.{1}.{2}",
                Major,
                Minor,
                Patch
            );
        }

        private static bool TryParseComponent(
            string value,
            out int component
        )
        {
            component = 0;

            if (string.IsNullOrEmpty(value))
                return false;

            return int.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out component
            );
        }
    }
}
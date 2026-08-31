using Mz.SemanticVersioning;

namespace Mz.TextTemplate
{
    /// <summary>
    /// Defines the package version and ordered changelog of this library.
    /// </summary>
    public static class LibraryVersionFile
    {
        /// <summary>
        /// Gets the major version number.
        /// </summary>
        public const int Major = 0;

        /// <summary>
        /// Gets the minor version number.
        /// </summary>
        public const int Minor = 1;

        /// <summary>
        /// Gets the patch version number.
        /// </summary>
        public const int Patch = 0;

        /// <summary>
        /// Gets the version string.
        /// </summary>
        public static string VersionString => Major + "." + Minor + "." + Patch;

        /// <summary>
        /// Gets the complete changelog ordered from newest to oldest.
        /// </summary>
        public static Changelog Changelog { get; } = new Changelog(
                VersionString,
                new[]
                {
                    new ChangelogEntry(
                        "0.1.0",
                        new[]
                        {
                            "Published the initial SELibs package release.",
                            "Added recoverable template parsing with exact source spans, arguments, and nested blocks.",
                            "Added host-defined language analysis, semantic syntax classifications, and strict argument contracts.",
                            "Added package documentation and Space Engineers source-copy validation."
                        }
                    )
                }
            );
    }
}

using System;
using Xunit;

namespace Mz.SemanticVersioning.Tests
{
    public sealed class ChangelogTests
    {
        [Fact]
        public void Entry_ParsesVersionAndCopiesChanges()
        {
            var changes = new[] { "Added feature." };
            var entry = new ChangelogEntry("1.2.3", changes);

            changes[0] = "Mutated input.";

            Assert.Equal(
                new SemanticVersion(1, 2, 3),
                entry.Version
            );
            Assert.Equal("Added feature.", entry.Changes[0]);

            var returnedChanges = entry.Changes;
            returnedChanges[0] = "Mutated output.";

            Assert.Equal("Added feature.", entry.Changes[0]);
        }

        [Fact]
        public void Entry_NullVersion_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ChangelogEntry(
                    null,
                    new[] { "Change." }
                )
            );

            Assert.Equal("version", exception.ParamName);
        }

        [Fact]
        public void Entry_InvalidVersion_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(
                () => new ChangelogEntry(
                    "1.2",
                    new[] { "Change." }
                )
            );
        }

        [Fact]
        public void Entry_NullChanges_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ChangelogEntry("1.2.3", null)
            );

            Assert.Equal("changes", exception.ParamName);
        }

        [Fact]
        public void Entry_EmptyChanges_ThrowsArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new ChangelogEntry(
                    "1.2.3",
                    Array.Empty<string>()
                )
            );

            Assert.Equal("changes", exception.ParamName);
        }

        [Fact]
        public void Entry_BlankChange_ThrowsArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new ChangelogEntry(
                    "1.2.3",
                    new[] { " " }
                )
            );

            Assert.Equal("changes", exception.ParamName);
        }

        [Fact]
        public void Changelog_StoresNewestFirstHistoryAndCopiesArray()
        {
            var entries = new[]
            {
                new ChangelogEntry(
                    "2.0.0",
                    new[] { "Current." }
                ),
                new ChangelogEntry(
                    "1.0.0",
                    new[] { "Previous." }
                )
            };

            var changelog = new Changelog("2.0.0", entries);

            entries[0] = entries[1];

            Assert.Equal(
                new SemanticVersion(2, 0, 0),
                changelog.CurrentVersion
            );
            Assert.Equal(
                new SemanticVersion(2, 0, 0),
                changelog.Current.Version
            );
            Assert.Equal(2, changelog.Entries.Length);

            var returnedEntries = changelog.Entries;
            returnedEntries[0] = returnedEntries[1];

            Assert.Equal(
                new SemanticVersion(2, 0, 0),
                changelog.Current.Version
            );
        }

        [Fact]
        public void Changelog_EmptyEntries_ThrowsArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new Changelog(
                    "1.0.0",
                    Array.Empty<ChangelogEntry>()
                )
            );

            Assert.Equal("entries", exception.ParamName);
        }

        [Fact]
        public void Changelog_NullEntry_ThrowsArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new Changelog(
                    "1.0.0",
                    new ChangelogEntry[] { null! }
                )
            );

            Assert.Equal("entries", exception.ParamName);
        }

        [Fact]
        public void Changelog_DuplicateVersion_ThrowsArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new Changelog(
                    "1.0.0",
                    new[]
                    {
                        new ChangelogEntry(
                            "1.0.0",
                            new[] { "One." }
                        ),
                        new ChangelogEntry(
                            "1.0.0",
                            new[] { "Duplicate." }
                        )
                    }
                )
            );

            Assert.Equal("entries", exception.ParamName);
        }

        [Fact]
        public void Changelog_OldestFirst_ThrowsArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new Changelog(
                    "1.0.0",
                    new[]
                    {
                        new ChangelogEntry(
                            "1.0.0",
                            new[] { "Older." }
                        ),
                        new ChangelogEntry(
                            "2.0.0",
                            new[] { "Newer." }
                        )
                    }
                )
            );

            Assert.Equal("entries", exception.ParamName);
        }

        [Fact]
        public void Changelog_CurrentVersionMismatch_ThrowsArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new Changelog(
                    "2.0.0",
                    new[]
                    {
                        new ChangelogEntry(
                            "1.0.0",
                            new[] { "Wrong." }
                        )
                    }
                )
            );

            Assert.Equal("entries", exception.ParamName);
        }
    }
}

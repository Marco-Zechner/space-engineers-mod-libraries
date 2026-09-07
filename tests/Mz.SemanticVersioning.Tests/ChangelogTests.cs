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

            Assert.Equal(new(1, 2, 3), entry.Version);
            Assert.Equal("Added feature.", entry.Changes[0]);

            string[]? returnedChanges = entry.Changes;
            returnedChanges[0] = "Mutated output.";

            Assert.Equal("Added feature.", entry.Changes[0]);
        }

        [Fact]
        public void Entry_NullVersion_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new ChangelogEntry(null, ["Change."]));

            Assert.Equal("version", exception.ParamName);
        }

        [Fact]
        public void Entry_InvalidVersion_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => new ChangelogEntry("1.2", ["Change."]));
        }

        [Fact]
        public void Entry_NullChanges_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new ChangelogEntry("1.2.3", null));

            Assert.Equal("changes", exception.ParamName);
        }

        [Fact]
        public void Entry_EmptyChanges_ThrowsArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(() => new ChangelogEntry("1.2.3", []));

            Assert.Equal("changes", exception.ParamName);
        }

        [Fact]
        public void Entry_BlankChange_ThrowsArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(() => new ChangelogEntry("1.2.3", [" "]));

            Assert.Equal("changes", exception.ParamName);
        }

        [Fact]
        public void Changelog_StoresNewestFirstHistoryAndCopiesArray()
        {
            ChangelogEntry[] entries =
            [
                new ChangelogEntry("2.0.0", ["Current."]),
                new ChangelogEntry("1.0.0", ["Previous."])
            ];

            var changelog = new Changelog("2.0.0", entries);

            entries[0] = entries[1];

            Assert.Equal(new(2, 0, 0), changelog.CurrentVersion);
            Assert.Equal(new(2, 0, 0), changelog.Current.Version);
            Assert.Equal(2, changelog.Entries.Length);

            var returnedEntries = changelog.Entries;
            returnedEntries[0] = returnedEntries[1];

            Assert.Equal(new(2, 0, 0), changelog.Current.Version);
        }

        [Fact]
        public void Changelog_EmptyEntries_ThrowsArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(() => new Changelog("1.0.0", []));

            Assert.Equal("entries", exception.ParamName);
        }

        [Fact]
        public void Changelog_NullEntry_ThrowsArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(() => new Changelog("1.0.0", [null!]));

            Assert.Equal("entries", exception.ParamName);
        }

        [Fact]
        public void Changelog_DuplicateVersion_ThrowsArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new Changelog(
                    "1.0.0",
                    [
                        new("1.0.0", ["One."]),
                        new("1.0.0", ["Duplicate."])
                    ]
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
                    [
                        new("1.0.0", ["Older."]),
                        new("2.0.0", ["Newer."])
                    ]
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
                    [new("1.0.0", ["Wrong."])]
                )
            );

            Assert.Equal("entries", exception.ParamName);
        }
    }
}

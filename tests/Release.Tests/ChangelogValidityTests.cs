using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceEngineersModLibraries.ReleaseTests;

public sealed class ChangelogValidityTests
{
    private static readonly Regex SemanticVersionPattern =
        new(
            @"^[0-9]+\.[0-9]+\.[0-9]+$",
            RegexOptions.Compiled
        );

    [Theory]
    [MemberData(
        nameof(ReleaseTestData.LibraryIds),
        MemberType = typeof(ReleaseTestData)
    )]
    public void ChangelogIsValid(
        string packageId
    )
    {
        LibraryReleaseDescriptor library =
            ReleaseRepository
                .Load()
                .GetLibrary(packageId);

        Assert.True(
            library.Changelog.Count > 0,
            $"Package '{packageId}' has no changelog entries."
        );

        Assert.Equal(
            library.Version,
            library.Changelog[0].Version
        );

        var seenVersions =
            new HashSet<string>(
                StringComparer.Ordinal
            );

        Version? previousVersion =
            null;

        foreach (
            ChangelogEntryDescriptor entry in
            library.Changelog
        )
        {
            Assert.Matches(
                SemanticVersionPattern,
                entry.Version
            );

            Assert.True(
                seenVersions.Add(entry.Version),
                $"Package '{packageId}' declares changelog version '{entry.Version}' more than once."
            );

            Version parsedVersion =
                Version.Parse(entry.Version);

            if (previousVersion != null)
            {
                Assert.True(
                    parsedVersion.CompareTo(previousVersion) < 0,
                    $"Package '{packageId}' changelog must be ordered from newest to oldest."
                );
            }

            Assert.True(
                entry.Changes.Count > 0,
                $"Package '{packageId}' changelog version '{entry.Version}' has no changes."
            );

            foreach (string change in entry.Changes)
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(change),
                    $"Package '{packageId}' changelog version '{entry.Version}' contains an empty change."
                );
            }

            previousVersion =
                parsedVersion;
        }
    }
}

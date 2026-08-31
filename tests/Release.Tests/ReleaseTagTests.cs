using System;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceEngineersModLibraries.ReleaseTests;

public sealed class ReleaseTagTests
{
    [Theory]
    [MemberData(
        nameof(ReleaseTestData.LibraryIds),
        MemberType = typeof(ReleaseTestData)
    )]
    public void CurrentVersionMatchesNewestLocalReleaseTagOrIsInitialUnreleasedVersion(
        string packageId
    )
    {
        ReleaseRepository repository =
            ReleaseRepository.Load();

        LibraryReleaseDescriptor library =
            repository.GetLibrary(packageId);

        string tagPrefix =
            "release/" + packageId + "/";

        string[] tags =
            repository.ListTags(
                tagPrefix + "*"
            );

        if (tags.Length == 0)
        {
            Assert.Equal(
                "0.1.0",
                library.Version
            );

            Assert.Single(
                library.Changelog
            );

            return;
        }

        var versionedTags =
            tags
                .Select(
                    tag =>
                    {
                        Match match =
                            Regex.Match(
                                tag,
                                (
                                    "^"
                                    + Regex.Escape(tagPrefix)
                                    + "(?<version>[0-9]+\\.[0-9]+\\.[0-9]+)$"
                                )
                            );

                        Assert.True(
                            match.Success,
                            $"Tag '{tag}' does not use the expected release format."
                        );

                        string versionText =
                            match.Groups["version"].Value;

                        return new
                        {
                            Tag = tag,
                            VersionText = versionText,
                            Version = Version.Parse(versionText)
                        };
                    }
                )
                .OrderByDescending(item => item.Version)
                .ToArray();

        Assert.Equal(
            library.Version,
            versionedTags[0].VersionText
        );

        string expectedTag =
            tagPrefix + library.Version;

        Assert.Contains(
            expectedTag,
            tags
        );

        string tagCommit =
            repository.ResolveTagCommit(
                expectedTag
            );

        string taggedVersionSource =
            repository.ReadFileAtCommit(
                tagCommit,
                library.RelativeVersionFilePath
            );

        LibraryReleaseDescriptor taggedLibrary =
            LibraryReleaseDescriptor.Parse(
                taggedVersionSource,
                library.RelativeVersionFilePath
            );

        Assert.Equal(
            library.PackageId,
            taggedLibrary.PackageId
        );

        Assert.Equal(
            library.Version,
            taggedLibrary.Version
        );

        Assert.Equal(
            library.FlattenChangelog(),
            taggedLibrary.FlattenChangelog()
        );
    }
}

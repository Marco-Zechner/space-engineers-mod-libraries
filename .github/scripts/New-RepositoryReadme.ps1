[CmdletBinding()]
param(
    [string]$Repository = (
        "Marco-Zechner/space-engineers-mod-libraries"
    ),

    [string]$OutputPath,

    [string]$ReleaseMetadataPath
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Write-Utf8WithoutBom {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Text
    )

    $encoding = New-Object System.Text.UTF8Encoding($false)
    $normalized = $Text.Replace("`r`n", "`n").Replace("`r", "`n")

    [System.IO.File]::WriteAllText(
        $Path,
        $normalized,
        $encoding
    )
}

function Get-RepositoryRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullRoot = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar
    )

    $prefix = $fullRoot + [System.IO.Path]::DirectorySeparatorChar

    if (
        -not $fullPath.StartsWith(
            $prefix,
            [System.StringComparison]::OrdinalIgnoreCase
        )
    ) {
        throw "Path '$fullPath' is outside repository '$fullRoot'."
    }

    return $fullPath.Substring($prefix.Length).Replace("\", "/")
}

function Get-GitHubHeaders {
    $headers = @{
        Accept = "application/vnd.github+json"
        "User-Agent" = "SpaceEngineersModLibraries-ReadmeGenerator"
        "X-GitHub-Api-Version" = "2022-11-28"
    }

    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_TOKEN)) {
        $headers["Authorization"] = "Bearer $env:GITHUB_TOKEN"
    }

    return $headers
}

function Get-ReleaseRecords {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repository,

        [string]$ReleaseMetadataPath
    )

    if (-not [string]::IsNullOrWhiteSpace($ReleaseMetadataPath)) {
        if (
            -not (
                Test-Path `
                    -LiteralPath $ReleaseMetadataPath `
                    -PathType Leaf
            )
        ) {
            throw "Release metadata file not found: $ReleaseMetadataPath"
        }

        $response = Get-Content `
            -LiteralPath $ReleaseMetadataPath `
            -Raw |
            ConvertFrom-Json
    }
    else {
        $response = Invoke-RestMethod `
            -Uri (
                "https://api.github.com/repos/" +
                "$Repository/releases?per_page=100"
            ) `
            -Headers (Get-GitHubHeaders) `
            -Method Get
    }

    return @(
        $response |
            ForEach-Object { $_ }
    )
}

function Read-ReleaseManifest {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Asset
    )

    $localPath = [string](
        Get-LibraryObjectPropertyValue `
            -Object $Asset `
            -Name "localPath"
    )

    if (-not [string]::IsNullOrWhiteSpace($localPath)) {
        if (-not (Test-Path -LiteralPath $localPath -PathType Leaf)) {
            throw "Fixture manifest not found: $localPath"
        }

        return Get-Content `
            -LiteralPath $localPath `
            -Raw |
            ConvertFrom-Json
    }

    $url = [string](
        Get-LibraryObjectPropertyValue `
            -Object $Asset `
            -Name "browser_download_url"
    )

    if ([string]::IsNullOrWhiteSpace($url)) {
        throw "Release manifest asset does not define a download URL."
    }

    return Invoke-RestMethod `
        -Uri $url `
        -Headers (Get-GitHubHeaders) `
        -Method Get
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $scriptDirectory "..\..")
)

. (Join-Path $scriptDirectory "LibraryReleaseMetadata.ps1")

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot "README.md"
}

$libraries = @(
    Get-ReleaseLibraries -RepoRoot $repoRoot |
        Sort-Object PackageId
)

$latestByPackageId = @{}
$tagPattern = (
    '^release/' +
    '(?<packageId>[A-Za-z_][A-Za-z0-9_.]*)/' +
    '(?<version>[0-9]+\.[0-9]+\.[0-9]+)$'
)

foreach (
    $release in @(
        Get-ReleaseRecords `
            -Repository $Repository `
            -ReleaseMetadataPath $ReleaseMetadataPath
    )
) {
    if (
        [bool](
            Get-LibraryObjectPropertyValue `
                -Object $release `
                -Name "draft"
        ) -or
        [bool](
            Get-LibraryObjectPropertyValue `
                -Object $release `
                -Name "prerelease"
        )
    ) {
        continue
    }

    $tag = [string](
        Get-LibraryObjectPropertyValue `
            -Object $release `
            -Name "tag_name"
    )

    if ($tag -notmatch $tagPattern) {
        continue
    }

    $tagPackageId = $Matches["packageId"]
    $versionText = $Matches["version"]

    $libraryMatches = @(
        $libraries |
            Where-Object {
                ([string]$_.PackageId).Equals(
                    $tagPackageId,
                    [System.StringComparison]::Ordinal
                )
            }
    )

    if ($libraryMatches.Count -ne 1) {
        continue
    }

    $library = $libraryMatches[0]
    $packageId = [string]$library.PackageId

    $assets = @(
        (
            Get-LibraryObjectPropertyValue `
                -Object $release `
                -Name "assets"
        ) |
            ForEach-Object { $_ }
    )

    $manifestAssets = @(
        $assets |
            Where-Object {
                ([string](
                    Get-LibraryObjectPropertyValue `
                        -Object $_ `
                        -Name "name"
                )).EndsWith(
                    "-package.json",
                    [System.StringComparison]::OrdinalIgnoreCase
                )
            }
    )

    if ($manifestAssets.Count -ne 1) {
        throw (
            "Published release '$tag' must contain exactly one " +
            "package manifest asset."
        )
    }

    $manifest = Read-ReleaseManifest -Asset $manifestAssets[0]

    if ([int]$manifest.schemaVersion -ne 1) {
        throw "Release '$tag' uses an unsupported manifest schema."
    }

    if (
        -not ([string]$manifest.id).Equals(
            $packageId,
            [System.StringComparison]::Ordinal
        )
    ) {
        throw (
            "Release '$tag' manifest ID '$($manifest.id)' does not " +
            "match '$packageId'."
        )
    }

    if ([string]$manifest.version -ne $versionText) {
        throw (
            "Release '$tag' manifest version '$($manifest.version)' " +
            "does not match its tag."
        )
    }

    $parsedVersion = [version]::Parse($versionText)
    $releaseUrl = [string](
        Get-LibraryObjectPropertyValue `
            -Object $release `
            -Name "html_url"
    )

    if ([string]::IsNullOrWhiteSpace($releaseUrl)) {
        $releaseUrl = (
            "https://github.com/$Repository/releases/tag/" +
            [System.Uri]::EscapeDataString($tag)
        )
    }

    $changes = @()
    $changelogProperty = $manifest.PSObject.Properties["changelog"]

    if ($null -ne $changelogProperty) {
        $changelog = @(
            $changelogProperty.Value |
                ForEach-Object { $_ }
        )

        if ($changelog.Count -eq 0) {
            throw "Release '$tag' publishes an empty changelog."
        }

        if ([string]$changelog[0].version -ne $versionText) {
            throw (
                "Release '$tag' changelog does not begin with " +
                "the released version."
            )
        }

        $changes = @(
            $changelog[0].changes |
                ForEach-Object { [string]$_ }
        )

        if (
            $changes.Count -eq 0 -or
            @(
                $changes |
                    Where-Object {
                        [string]::IsNullOrWhiteSpace($_)
                    }
            ).Count -ne 0
        ) {
            throw (
                "Release '$tag' current changelog contains no " +
                "usable changes."
            )
        }
    }

    $candidate = [pscustomobject]@{
        PackageId = $packageId
        Version = $versionText
        ParsedVersion = $parsedVersion
        Url = $releaseUrl
        Changes = $changes
    }

    if (
        -not $latestByPackageId.ContainsKey($packageId) -or
        $candidate.ParsedVersion.CompareTo(
            $latestByPackageId[$packageId].ParsedVersion
        ) -gt 0
    ) {
        $latestByPackageId[$packageId] = $candidate
    }
}

$lines = New-Object System.Collections.ArrayList

[void]$lines.Add("# Space Engineers Mod Libraries")
[void]$lines.Add("")
[void]$lines.Add(
    "Reusable source libraries for Space Engineers mod projects."
)
[void]$lines.Add("")
[void]$lines.Add(
    "Packages are distributed through " +
    "[SELibs](https://github.com/Marco-Zechner/selibs), " +
    "which resolves exact dependencies and installs source folders."
)
[void]$lines.Add("")
[void]$lines.Add(
    "> This README is generated from published stable GitHub releases. " +
    "Do not edit it manually."
)
[void]$lines.Add("")
[void]$lines.Add("## Packages")
[void]$lines.Add("")
[void]$lines.Add(
    "| Package | Latest stable release | Documentation |"
)
[void]$lines.Add("| --- | --- | --- |")

foreach ($library in $libraries) {
    $packageId = [string]$library.PackageId
    $readmePath = Join-Path `
        (Split-Path -Parent ([string]$library.RootProjectPath)) `
        "README.md"

    $documentation = if (
        Test-Path -LiteralPath $readmePath -PathType Leaf
    ) {
        $relativeReadme = Get-RepositoryRelativePath `
            -Path $readmePath `
            -RepoRoot $repoRoot

        "[Guide]($relativeReadme)"
    }
    else {
        "Not available"
    }

    $releaseText = if ($latestByPackageId.ContainsKey($packageId)) {
        $release = $latestByPackageId[$packageId]
        "[``$($release.Version)``]($($release.Url))"
    }
    else {
        "Not published"
    }

    [void]$lines.Add(
        "| ``$($library.PackageId)`` | $releaseText | $documentation |"
    )
}

[void]$lines.Add("")
[void]$lines.Add("## Latest changes")

foreach ($library in $libraries) {
    $packageId = [string]$library.PackageId

    [void]$lines.Add("")
    [void]$lines.Add("### $($library.PackageId)")
    [void]$lines.Add("")

    if (-not $latestByPackageId.ContainsKey($packageId)) {
        [void]$lines.Add("- No stable release has been published.")
        continue
    }

    $release = $latestByPackageId[$packageId]

    [void]$lines.Add(
        "- Latest stable release: " +
        "[``$($release.Version)``]($($release.Url))"
    )

    if (@($release.Changes).Count -eq 0) {
        [void]$lines.Add(
            "- No changelog metadata was published for this release."
        )
    }
    else {
        foreach ($change in @($release.Changes)) {
            [void]$lines.Add("- $change")
        }
    }
}

[void]$lines.Add("")
[void]$lines.Add("## Release format")
[void]$lines.Add("")
[void]$lines.Add(
    "Each stable release publishes a SELibs package manifest and a " +
    "checksum-verified source component archive."
)
[void]$lines.Add("")
[void]$lines.Add(
    "See [SELibs package releases](docs/SELibs-Packages.md) for tag, " +
    "asset, dependency, and package-discovery details."
)

Write-Utf8WithoutBom `
    -Path ([System.IO.Path]::GetFullPath($OutputPath)) `
    -Text (($lines -join "`n") + "`n")

Write-Output "Generated repository README: $OutputPath"

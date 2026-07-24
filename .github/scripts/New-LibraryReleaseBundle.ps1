[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Tag,

    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory
)

$ErrorActionPreference = "Stop"

function Write-Utf8WithoutBom {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [string] $Text
    )

    $encoding = New-Object System.Text.UTF8Encoding($false)

    [System.IO.File]::WriteAllText(
        $Path,
        $Text,
        $encoding
    )
}

function Write-GitHubOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,

        [Parameter(Mandatory = $true)]
        [string] $Value
    )

    if ([string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
        return
    }

    [System.IO.File]::AppendAllText(
        $env:GITHUB_OUTPUT,
        $Name + "=" + $Value + [Environment]::NewLine
    )
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path

$repoRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $scriptDirectory "..\..")
)

$manifestPath = Join-Path $repoRoot (
    ".github\release-libraries.json"
)

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Release manifest not found: $manifestPath"
}

$tagPattern =
    '^(?<slug>apiprotocol|logging|networking|semanticversioning)' +
    '/v(?<version>[0-9]+\.[0-9]+\.[0-9]+' +
    '(?:-[0-9A-Za-z][0-9A-Za-z.-]*)?)' +
    '-r(?<revision>[1-9][0-9]*)$'

if ($Tag -notmatch $tagPattern) {
    throw (
        "Invalid release tag '$Tag'. Expected " +
        "<library>/v<version>-r<revision>, for example " +
        "apiprotocol/v0.2.0-r1."
    )
}

$slug = $Matches["slug"].ToLowerInvariant()
$version = $Matches["version"]
$revision = [int]$Matches["revision"]

$manifest = Get-Content `
    -LiteralPath $manifestPath `
    -Raw |
    ConvertFrom-Json

$libraryProperty = $manifest.PSObject.Properties[$slug]

if ($libraryProperty -eq $null) {
    throw "Library '$slug' is not declared in the release manifest."
}

$library = $libraryProperty.Value
$displayName = [string]$library.displayName
$testProjectPrefix = [string]$library.testProjectPrefix

if ([string]::IsNullOrWhiteSpace($displayName)) {
    throw "Library '$slug' has no display name."
}

$sourceDirectories = @($library.sourceDirectories)

if ($sourceDirectories.Count -eq 0) {
    throw "Library '$slug' has no source directories."
}

Write-Output "Library: $displayName"
Write-Output "Version: $version"
Write-Output "Release revision: $revision"
Write-Output "Tag: $Tag"

$testRoot = Join-Path $repoRoot "tests"

$testProjects = @(
    Get-ChildItem `
        -LiteralPath $testRoot `
        -Recurse `
        -File `
        -Filter "*.csproj" |
        Where-Object {
            $_.BaseName.StartsWith(
                $testProjectPrefix,
                [System.StringComparison]::Ordinal
            ) `
                -and $_.BaseName.EndsWith(
                    ".Tests",
                    [System.StringComparison]::Ordinal
                ) `
                -and -not $_.BaseName.EndsWith(
                    ".SpaceEngineers.Tests",
                    [System.StringComparison]::Ordinal
                )
        } |
        Sort-Object FullName
)

if ($testProjects.Count -eq 0) {
    throw "No portable test projects found for $displayName."
}

Write-Output ""
Write-Output "Portable test projects:"

foreach ($testProject in $testProjects) {
    Write-Output "  $($testProject.BaseName)"
}

foreach ($testProject in $testProjects) {
    Write-Output ""
    Write-Output "Running $($testProject.BaseName)"

    & dotnet test $testProject.FullName `
        --configuration Release `
        --nologo `
        --verbosity minimal

    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed: $($testProject.FullName)"
    }
}

$outputFull = [System.IO.Path]::GetFullPath($OutputDirectory)

if (Test-Path -LiteralPath $outputFull) {
    Remove-Item `
        -LiteralPath $outputFull `
        -Recurse `
        -Force
}

New-Item `
    -ItemType Directory `
    -Path $outputFull |
    Out-Null

$safeVersion = $version -replace '[^0-9A-Za-z.-]', '_'

$assetBaseName =
    $displayName +
    "-v" +
    $safeVersion +
    "-r" +
    $revision +
    "-source"

$packageRoot = Join-Path $outputFull $assetBaseName
$librariesRoot = Join-Path $packageRoot "Libraries"

New-Item `
    -ItemType Directory `
    -Path $librariesRoot `
    -Force |
    Out-Null

$copiedFileCount = 0

foreach ($sourceRelative in $sourceDirectories) {
    $sourceDirectory = Join-Path $repoRoot (
        ([string]$sourceRelative).Replace("/", "\")
    )

    if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container)) {
        throw "Source directory not found: $sourceDirectory"
    }

    $destinationDirectory = Join-Path $librariesRoot (
        Split-Path -Leaf $sourceDirectory
    )

    $sourceFiles = @(
        Get-ChildItem `
            -LiteralPath $sourceDirectory `
            -Recurse `
            -File `
            -Filter "*.cs" |
            Where-Object {
                $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
            } |
            Sort-Object FullName
    )

    if ($sourceFiles.Count -eq 0) {
        throw "No C# source files found in: $sourceDirectory"
    }

    foreach ($sourceFile in $sourceFiles) {
        $relativePath = $sourceFile.FullName.Substring(
            $sourceDirectory.Length
        ).TrimStart(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar
        )

        $destinationPath = Join-Path `
            $destinationDirectory `
            $relativePath

        $destinationParent = Split-Path -Parent $destinationPath

        New-Item `
            -ItemType Directory `
            -Path $destinationParent `
            -Force |
            Out-Null

        Copy-Item `
            -LiteralPath $sourceFile.FullName `
            -Destination $destinationPath

        $copiedFileCount++
    }
}

if ($copiedFileCount -eq 0) {
    throw "The release bundle contains no source files."
}

$commit = (& git -C $repoRoot rev-parse HEAD)

if ($LASTEXITCODE -ne 0) {
    throw "Could not determine the release commit."
}

$readmeLines = @(
    "$displayName source-copy release",
    "",
    "Library version: $version",
    "Release revision: $revision",
    "Tag: $Tag",
    "Commit: $commit",
    "",
    "This archive contains source-copy libraries under Libraries/.",
    "Copy the required library folders into the consuming mod project.",
    "",
    "This archive does not contain runtime DLL dependencies."
)

Write-Utf8WithoutBom `
    -Path (Join-Path $packageRoot "README.txt") `
    -Text (($readmeLines -join [Environment]::NewLine) + [Environment]::NewLine)

$packagedFiles = @(
    Get-ChildItem `
        -LiteralPath $packageRoot `
        -Recurse `
        -File |
        Sort-Object FullName |
        ForEach-Object {
            $_.FullName.Substring(
                $packageRoot.Length
            ).TrimStart(
                [System.IO.Path]::DirectorySeparatorChar,
                [System.IO.Path]::AltDirectorySeparatorChar
            ).Replace("\", "/")
        }
)

$metadata = [ordered]@{
    library = $displayName
    slug = $slug
    version = $version
    releaseRevision = $revision
    tag = $Tag
    commit = $commit
    sourceDirectories = @($sourceDirectories)
    files = @($packagedFiles)
}

Write-Utf8WithoutBom `
    -Path (Join-Path $packageRoot "release.json") `
    -Text (
        ($metadata | ConvertTo-Json -Depth 6) +
        [Environment]::NewLine
    )

$notesPath = Join-Path $outputFull "release-notes.md"

$notesLines = @(
    "# $displayName $version (release $revision)",
    "",
    "Source-copy bundle generated from commit ``$commit``.",
    "",
    "## Validation",
    "",
    "Portable test projects executed:"
)

foreach ($testProject in $testProjects) {
    $notesLines += "- ``$($testProject.BaseName)``"
}

$notesLines += @(
    "",
    "Space Engineers-specific tests and MDK source-copy validation are not",
    "executed on GitHub-hosted runners because the required game assemblies",
    "are not stored in this repository.",
    "",
    "## Asset",
    "",
    "The ZIP contains the relevant source-copy folders under ``Libraries/``."
)

Write-Utf8WithoutBom `
    -Path $notesPath `
    -Text (($notesLines -join [Environment]::NewLine) + [Environment]::NewLine)

$zipPath = Join-Path $outputFull ($assetBaseName + ".zip")

Compress-Archive `
    -Path $packageRoot `
    -DestinationPath $zipPath `
    -CompressionLevel Optimal

$hash = Get-FileHash `
    -LiteralPath $zipPath `
    -Algorithm SHA256

$checksumPath = $zipPath + ".sha256"

Write-Utf8WithoutBom `
    -Path $checksumPath `
    -Text (
        $hash.Hash.ToLowerInvariant() +
        "  " +
        (Split-Path -Leaf $zipPath) +
        [Environment]::NewLine
    )

$isPrerelease = $version.Contains("-")

$releaseTitle =
    $displayName +
    " " +
    $version +
    " (release " +
    $revision +
    ")"

Write-GitHubOutput `
    -Name "asset_path" `
    -Value $zipPath

Write-GitHubOutput `
    -Name "checksum_path" `
    -Value $checksumPath

Write-GitHubOutput `
    -Name "release_notes_path" `
    -Value $notesPath

Write-GitHubOutput `
    -Name "release_title" `
    -Value $releaseTitle

Write-GitHubOutput `
    -Name "is_prerelease" `
    -Value $isPrerelease.ToString().ToLowerInvariant()

Write-Output ""
Write-Output "Release bundle created:"
Write-Output "  ZIP: $zipPath"
Write-Output "  SHA256: $checksumPath"
Write-Output "  Source files: $copiedFileCount"

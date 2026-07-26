[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Tag,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [switch]$SkipTests
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

function Write-GitHubOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [string]$Value
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
. (Join-Path $scriptDirectory "LibraryReleaseMetadata.ps1")

$tagPattern = (
    '^release/' +
    '(?<packageId>[A-Za-z_][A-Za-z0-9_.]*)/' +
    '(?<version>[0-9]+\.[0-9]+\.[0-9]+)$'
)

if ($Tag -notmatch $tagPattern) {
    throw (
        "Invalid release tag '$Tag'. Expected " +
        "release/<namespace>/<major.minor.patch>, for example " +
        "release/Mz.ApiProtocol/0.2.0."
    )
}

$tagPackageId = $Matches["packageId"]
$version = $Matches["version"]

$libraries = @(Get-ReleaseLibraries -RepoRoot $repoRoot)

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
    throw (
        "Library namespace '$tagPackageId' was not discovered exactly once."
    )
}
$library = $libraryMatches[0]
$packageId = [string]$library.PackageId
$sourceDirectories = @($library.SourceDirectories)
$changelog = @($library.Changelog)

if ($version -ne [string]$library.Version) {
    throw (
        "Release tag version '$version' does not match " +
        "LibraryVersionFile version '$($library.Version)' for " +
        "'$packageId'."
    )
}

$dependencies = Resolve-LibraryDependencies `
    -Library $library `
    -Libraries $libraries `
    -RepoRoot $repoRoot

$folders = New-Object System.Collections.ArrayList
$folderClaims = @{}

foreach ($sourceRelativeValue in $sourceDirectories) {
    $sourceRelative = [string]$sourceRelativeValue

    $sourceDirectory = Join-Path `
        $repoRoot `
        $sourceRelative.Replace("/", "\")

    if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container)) {
        throw "Source directory not found: $sourceDirectory"
    }

    $folder = Split-Path -Leaf $sourceDirectory

    if ($folder -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$') {
        throw "Source folder '$folder' is not a valid SELibs folder name."
    }

    $key = $folder.ToLowerInvariant()

    if ($folderClaims.ContainsKey($key)) {
        throw "Source folder '$folder' is declared more than once."
    }

    $folderClaims[$key] = $true
    [void]$folders.Add($folder)
}
Write-Output "Package: $packageId"
Write-Output "Version: $version"
Write-Output "Tag: $Tag"

$testProjects = @(
    Get-LibraryPortableTestProjects `
        -Library $library `
        -RepoRoot $repoRoot
)

if ($testProjects.Count -eq 0) {
    throw "No portable test projects found for $packageId."
}

Write-Output ""
Write-Output "Portable test projects:"

foreach ($testProject in $testProjects) {
    Write-Output "  $($testProject.BaseName)"
}

if (-not $SkipTests) {
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
}
else {
    Write-Output ""
    Write-Output "Portable tests were skipped by request."
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

$stagingRoot = Join-Path $outputFull ".staging"
$librariesRoot = Join-Path $stagingRoot "Libraries"

New-Item `
    -ItemType Directory `
    -Path $librariesRoot `
    -Force |
    Out-Null

$copiedFileCount = 0
$copiedReadmeCount = 0

try {
    foreach ($sourceRelativeValue in $sourceDirectories) {
        $sourceRelative = [string]$sourceRelativeValue
        $sourceDirectory = Join-Path `
            $repoRoot `
            $sourceRelative.Replace("/", "\")

        $destinationDirectory = Join-Path `
            $librariesRoot `
            (Split-Path -Leaf $sourceDirectory)

        $sourceFiles = @(
            Get-ChildItem `
                -LiteralPath $sourceDirectory `
                -Recurse `
                -File |
                Where-Object {
                    $_.FullName -notmatch '[\\/](bin|obj)[\\/]' `
                        -and (
                            $_.Extension.Equals(
                                ".cs",
                                [System.StringComparison]::OrdinalIgnoreCase
                            ) `
                            -or $_.Name.Equals(
                                "README.md",
                                [System.StringComparison]::OrdinalIgnoreCase
                            ) `
                            -or $_.Name.Equals(
                                "Guide.md",
                                [System.StringComparison]::OrdinalIgnoreCase
                            )
                        )
                } |
                Sort-Object FullName
        )

        $csharpSourceCount = @(
            $sourceFiles |
                Where-Object {
                    $_.Extension.Equals(
                        ".cs",
                        [System.StringComparison]::OrdinalIgnoreCase
                    )
                }
        ).Count

        if ($csharpSourceCount -eq 0) {
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

            if (
                $sourceFile.Name.Equals(
                    "README.md",
                    [System.StringComparison]::OrdinalIgnoreCase
                )
            ) {
                $copiedReadmeCount++
            }
        }
    }

    if ($copiedFileCount -eq 0) {
        throw "The component archive contains no source files."
    }

    if ($copiedReadmeCount -eq 0) {
        throw "The component archive contains no package README.md."
    }

    $assetBaseName = "$packageId-$version"
    $componentName = "$assetBaseName-component.zip"
    $manifestName = "$assetBaseName-package.json"
    $componentPath = Join-Path $outputFull $componentName
    $manifestPath = Join-Path $outputFull $manifestName

    Compress-Archive `
        -LiteralPath $librariesRoot `
        -DestinationPath $componentPath `
        -CompressionLevel Optimal

    $componentHash = (
        Get-FileHash `
            -LiteralPath $componentPath `
            -Algorithm SHA256
    ).Hash.ToLowerInvariant()

    $packageManifest = [ordered]@{
        schemaVersion = 1
        id = $packageId
        version = $version
        changelog = @(
            $changelog |
                ForEach-Object {
                    [ordered]@{
                        version = [string]$_.Version
                        changes = @($_.Changes)
                    }
                }
        )
        dependencies = $dependencies
        folders = @($folders | Sort-Object)
        component = [ordered]@{
            asset = $componentName
            sha256 = $componentHash
        }
    }

    Write-Utf8WithoutBom `
        -Path $manifestPath `
        -Text (
            ($packageManifest | ConvertTo-Json -Depth 10) +
            "`n"
        )

    $commit = (& git -C $repoRoot rev-parse HEAD)

    if ($LASTEXITCODE -ne 0) {
        throw "Could not determine the release commit."
    }

    $notesPath = Join-Path $outputFull "release-notes.md"
    $notesLines = @(
        "# $packageId $version",
        "",
        "SELibs source package generated from commit ``$commit``.",
        "",
        "## Changes",
        ""
    )

    foreach ($change in @($changelog[0].Changes)) {
        $notesLines += "- $change"
    }

    $notesLines += @(
        "",
        "## Package",
        "",
        "- ID: ``$packageId``",
        "- Version: ``$version``",
        "- Component: ``$componentName``",
        "- SHA-256: ``$componentHash``",
        "",
        "## Included folders"
    )

    foreach ($folder in @($folders | Sort-Object)) {
        $notesLines += "- ``Libraries/$folder``"
    }

    $notesLines += @(
        "",
        "## Exact dependencies"
    )

    if ($dependencies.Count -eq 0) {
        $notesLines += "- None"
    }
    else {
        foreach ($dependencyId in @($dependencies.Keys | Sort-Object)) {
            $notesLines += (
                "- ``$dependencyId`` ``$($dependencies[$dependencyId])``"
            )
        }
    }

    $notesLines += @(
        "",
        "## Validation",
        "",
        "Portable test projects:"
    )

    foreach ($testProject in $testProjects) {
        $notesLines += "- ``$($testProject.BaseName)``"
    }

    $notesLines += @(
        "",
        "Space Engineers-specific tests and MDK source-copy validation are not",
        "executed on GitHub-hosted runners because the required game assemblies",
        "are not stored in this repository."
    )

    Write-Utf8WithoutBom `
        -Path $notesPath `
        -Text (($notesLines -join "`n") + "`n")

    Write-GitHubOutput `
        -Name "component_path" `
        -Value $componentPath

    Write-GitHubOutput `
        -Name "manifest_path" `
        -Value $manifestPath

    Write-GitHubOutput `
        -Name "release_notes_path" `
        -Value $notesPath

    Write-GitHubOutput `
        -Name "release_title" `
        -Value "$packageId $version"

    Write-GitHubOutput `
        -Name "is_prerelease" `
        -Value "false"

    Write-Output ""
    Write-Output "SELibs release assets created:"
    Write-Output "  Manifest: $manifestPath"
    Write-Output "  Component: $componentPath"
    Write-Output "  SHA256: $componentHash"
    Write-Output "  Package files: $copiedFileCount"
    Write-Output "  README files: $copiedReadmeCount"
}
finally {
    if (Test-Path -LiteralPath $stagingRoot) {
        Remove-Item `
            -LiteralPath $stagingRoot `
            -Recurse `
            -Force
    }
}

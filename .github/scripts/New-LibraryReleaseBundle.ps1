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

function ConvertTo-DependencyMap {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Dependencies
    )

    $result = [ordered]@{}

    foreach (
        $property in @(
            $Dependencies.PSObject.Properties |
                Sort-Object Name
        )
    ) {
        $packageId = [string]$property.Name
        $version = [string]$property.Value

        if ($packageId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$') {
            throw "Dependency package ID '$packageId' is invalid."
        }

        if ($version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+$') {
            throw (
                "Dependency '$packageId' must use an exact numeric " +
                "major.minor.patch version."
            )
        }

        $result[$packageId] = $version
    }

    return $result
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $scriptDirectory "..\..")
)

$configurationPath = Join-Path `
    $repoRoot `
    ".github\release-libraries.json"

if (-not (Test-Path -LiteralPath $configurationPath -PathType Leaf)) {
    throw "Release configuration not found: $configurationPath"
}

$tagPattern = (
    '^(?<slug>[a-z0-9][a-z0-9-]*)/' +
    'v(?<version>[0-9]+\.[0-9]+\.[0-9]+)$'
)

if ($Tag -notmatch $tagPattern) {
    throw (
        "Invalid release tag '$Tag'. Expected " +
        "<library>/v<major.minor.patch>, for example " +
        "apiprotocol/v0.2.0."
    )
}

$slug = $Matches["slug"].ToLowerInvariant()
$version = $Matches["version"]

$configuration = Get-Content `
    -LiteralPath $configurationPath `
    -Raw |
    ConvertFrom-Json

$libraryProperty = @(
    $configuration.PSObject.Properties |
        Where-Object {
            $_.Name.Equals(
                $slug,
                [System.StringComparison]::OrdinalIgnoreCase
            )
        }
)

if ($libraryProperty.Count -ne 1) {
    throw "Library '$slug' is not declared exactly once."
}

$library = $libraryProperty[0].Value
$packageId = [string]$library.packageId
$testProjectPrefix = [string]$library.testProjectPrefix

if ($packageId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$') {
    throw "Library '$slug' has invalid packageId '$packageId'."
}

if ([string]::IsNullOrWhiteSpace($testProjectPrefix)) {
    throw "Library '$slug' has no testProjectPrefix."
}

$sourceDirectories = @($library.sourceDirectories)

if ($sourceDirectories.Count -eq 0) {
    throw "Library '$slug' has no source directories."
}

if ($null -eq $library.dependencies) {
    throw "Library '$slug' must declare a dependencies object."
}

$dependencies = ConvertTo-DependencyMap `
    -Dependencies $library.dependencies

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
            ) -and
            $_.BaseName.EndsWith(
                ".Tests",
                [System.StringComparison]::Ordinal
            ) -and
            -not $_.BaseName.EndsWith(
                ".SpaceEngineers.Tests",
                [System.StringComparison]::Ordinal
            )
        } |
        Sort-Object FullName
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
        throw "The component archive contains no source files."
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
    Write-Output "  Source files: $copiedFileCount"
}
finally {
    if (Test-Path -LiteralPath $stagingRoot) {
        Remove-Item `
            -LiteralPath $stagingRoot `
            -Recurse `
            -Force
    }
}

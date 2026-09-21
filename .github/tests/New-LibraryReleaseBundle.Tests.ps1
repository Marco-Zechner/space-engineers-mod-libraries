$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Add-Type -AssemblyName System.IO.Compression.FileSystem

$repoRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..\..")
)

$bundleScript = Join-Path `
    $repoRoot `
    ".github\scripts\New-LibraryReleaseBundle.ps1"

$metadataScript = Join-Path `
    $repoRoot `
    ".github\scripts\LibraryReleaseMetadata.ps1"

. $metadataScript

$libraries = @(
    Get-ReleaseLibraries -RepoRoot $repoRoot
)

function Get-TestLibrary {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageId
    )

    $matches = @(
        $libraries |
            Where-Object {
                ([string]$_.PackageId).Equals(
                    $PackageId,
                    [System.StringComparison]::Ordinal
                )
            }
    )

    if ($matches.Count -ne 1) {
        throw (
            "Expected exactly one discovered package named '{0}'; found {1}." -f
            $PackageId,
            $matches.Count
        )
    }

    return $matches[0]
}

$script:Passed = 0

function Assert-True {
    param(
        [Parameter(Mandatory = $true)]
        [bool]$Condition,

        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }

    $script:Passed++
}

function Assert-Equal {
    param(
        [Parameter(Mandatory = $true)]
        $Expected,

        [Parameter(Mandatory = $true)]
        $Actual,

        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if ($Expected -ne $Actual) {
        throw (
            "$Message`n" +
            "Expected: '$Expected'`n" +
            "Actual:   '$Actual'"
        )
    }

    $script:Passed++
}

function Assert-Throws {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action,

        [Parameter(Mandatory = $true)]
        [string]$ExpectedMessagePart
    )

    try {
        & $Action
    }
    catch {
        if ($_.Exception.Message -notlike "*$ExpectedMessagePart*") {
            throw (
                "Expected error containing '$ExpectedMessagePart', " +
                "but received '$($_.Exception.Message)'."
            )
        }

        $script:Passed++
        return
    }

    throw "Expected an exception containing '$ExpectedMessagePart'."
}

function Get-ZipEntries {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)

    try {
        return @(
            $archive.Entries |
                ForEach-Object {
                    $_.FullName.Replace("\", "/")
                } |
                Sort-Object
        )
    }
    finally {
        $archive.Dispose()
    }
}

$testRoot = Join-Path `
    ([System.IO.Path]::GetTempPath()) `
    ("selibs-package-format-" + [Guid]::NewGuid().ToString("N"))

New-Item -ItemType Directory -Path $testRoot | Out-Null

try {
    $semanticLibrary =
        Get-TestLibrary `
            -PackageId "Mz.SemanticVersioning"

    $semanticVersion =
        [string]$semanticLibrary.Version

    $semanticTag =
        "release/Mz.SemanticVersioning/$semanticVersion"

    $semanticChangelogVersions =
        @($semanticLibrary.Changelog.Version) -join ","

    $semanticCurrentChange =
        [string]$semanticLibrary.Changelog[0].Changes[0]

    $collectionsLibrary =
        Get-TestLibrary `
            -PackageId "Mz.Collections"

    $collectionsVersion =
        [string]$collectionsLibrary.Version

    $collectionsTag =
        "release/Mz.Collections/$collectionsVersion"

    $collectionsChangelogVersions =
        @($collectionsLibrary.Changelog.Version) -join ","

    $apiLibrary =
        Get-TestLibrary `
            -PackageId "Mz.ApiProtocol"

    $apiVersion =
        [string]$apiLibrary.Version

    $apiTag =
        "release/Mz.ApiProtocol/$apiVersion"

    $apiChangelogVersions =
        @($apiLibrary.Changelog.Version) -join ","

    $loggingLibrary =
        Get-TestLibrary `
            -PackageId "Mz.Logging"

    $loggingVersion =
        [string]$loggingLibrary.Version

    $loggingTag =
        "release/Mz.Logging/$loggingVersion"

    $networkingLibrary =
        Get-TestLibrary `
            -PackageId "Mz.Networking"

    $networkingVersion =
        [string]$networkingLibrary.Version

    $networkingTag =
        "release/Mz.Networking/$networkingVersion"

    $networkingChangelogVersions =
        @($networkingLibrary.Changelog.Version) -join ","

    $storageLibrary =
        Get-TestLibrary `
            -PackageId "Mz.Storage"

    $storageVersion =
        [string]$storageLibrary.Version

    $storageTag =
        "release/Mz.Storage/$storageVersion"

    $storageChangelogVersions =
        @($storageLibrary.Changelog.Version) -join ","
    $tomlLibrary =
        Get-TestLibrary `
            -PackageId "Mz.Toml"

    $tomlVersion =
        [string]$tomlLibrary.Version

    $tomlTag =
        "release/Mz.Toml/$tomlVersion"

    $tomlChangelogVersions =
        @($tomlLibrary.Changelog.Version) -join ","

    $semanticOutput = Join-Path $testRoot "semantic"

    & $bundleScript `
        -Tag $semanticTag `
        -OutputDirectory $semanticOutput `
        -SkipTests |
        Out-Null

    $semanticManifestPath = Join-Path `
        $semanticOutput `
        ("Mz.SemanticVersioning-" + $semanticVersion + "-package.json")

    $semanticComponentPath = Join-Path `
        $semanticOutput `
        ("Mz.SemanticVersioning-" + $semanticVersion + "-component.zip")

    Assert-True `
        -Condition (
            Test-Path `
                -LiteralPath $semanticManifestPath `
                -PathType Leaf
        ) `
        -Message "SemanticVersioning package manifest is missing."

    Assert-True `
        -Condition (
            Test-Path `
                -LiteralPath $semanticComponentPath `
                -PathType Leaf
        ) `
        -Message "SemanticVersioning component archive is missing."

    $semanticManifest = Get-Content `
        -LiteralPath $semanticManifestPath `
        -Raw |
        ConvertFrom-Json

    Assert-Equal `
        -Expected 1 `
        -Actual ([int]$semanticManifest.schemaVersion) `
        -Message "SemanticVersioning has the wrong schema version."

    Assert-Equal `
        -Expected "Mz.SemanticVersioning" `
        -Actual ([string]$semanticManifest.id) `
        -Message "SemanticVersioning has the wrong package ID."

    Assert-Equal `
        -Expected $semanticVersion `
        -Actual ([string]$semanticManifest.version) `
        -Message "SemanticVersioning has the wrong version."

    Assert-Equal `
        -Expected $semanticChangelogVersions `
        -Actual (
            @($semanticManifest.changelog.version) -join ","
        ) `
        -Message "SemanticVersioning changelog order is incorrect."

    Assert-Equal `
        -Expected $semanticCurrentChange `
        -Actual (
            [string]$semanticManifest.changelog[0].changes[0]
        ) `
        -Message "SemanticVersioning current changelog is incorrect."

    $semanticNotes = Get-Content `
        -LiteralPath (Join-Path $semanticOutput "release-notes.md") `
        -Raw

    Assert-True `
        -Condition ($semanticNotes.Contains("## Changes")) `
        -Message "Release notes do not contain a Changes section."

    Assert-True `
        -Condition (
            $semanticNotes.Contains(
                "- " + $semanticCurrentChange
            )
        ) `
        -Message "Release notes do not contain the current changelog."

    Assert-Equal `
        -Expected 0 `
        -Actual @(
            $semanticManifest.dependencies.PSObject.Properties
        ).Count `
        -Message "SemanticVersioning unexpectedly declares dependencies."

    Assert-Equal `
        -Expected "Mz.SemanticVersioning" `
        -Actual ([string]$semanticManifest.folders[0]) `
        -Message "SemanticVersioning declares the wrong folder."

    $semanticHash = (
        Get-FileHash `
            -LiteralPath $semanticComponentPath `
            -Algorithm SHA256
    ).Hash.ToLowerInvariant()

    Assert-Equal `
        -Expected $semanticHash `
        -Actual ([string]$semanticManifest.component.sha256) `
        -Message "SemanticVersioning manifest checksum is incorrect."

    $semanticEntries = @(Get-ZipEntries -Path $semanticComponentPath)

    Assert-True `
        -Condition ($semanticEntries.Count -gt 0) `
        -Message "SemanticVersioning archive is empty."

    Assert-True `
        -Condition (
            @(
                $semanticEntries |
                    Where-Object {
                        -not $_.StartsWith(
                            "Libraries/",
                            [System.StringComparison]::Ordinal
                        )
                    }
            ).Count -eq 0
        ) `
        -Message "SemanticVersioning archive has entries outside Libraries/."

    Assert-True `
        -Condition (
            $semanticEntries -contains
            "Libraries/Mz.SemanticVersioning/SemanticVersion.cs"
        ) `
        -Message "SemanticVersioning source file is missing from its archive."

    Assert-True `
        -Condition (
            $semanticEntries -contains
            "Libraries/Mz.SemanticVersioning/Changelog.cs"
        ) `
        -Message "Shared Changelog source is missing from its archive."

    Assert-True `
        -Condition (
            $semanticEntries -contains
            "Libraries/Mz.SemanticVersioning/ChangelogEntry.cs"
        ) `
        -Message "Shared ChangelogEntry source is missing from its archive."

    Assert-True `
        -Condition (
            $semanticEntries -contains
            "Libraries/Mz.SemanticVersioning/README.md"
        ) `
        -Message "SemanticVersioning README is missing from its archive."

    $collectionsOutput = Join-Path $testRoot "collections"

    & $bundleScript `
        -Tag $collectionsTag `
        -OutputDirectory $collectionsOutput `
        -SkipTests |
        Out-Null

    $collectionsManifestPath = Join-Path `
        $collectionsOutput `
        ("Mz.Collections-" + $collectionsVersion + "-package.json")

    $collectionsComponentPath = Join-Path `
        $collectionsOutput `
        ("Mz.Collections-" + $collectionsVersion + "-component.zip")

    $collectionsManifest = Get-Content `
        -LiteralPath $collectionsManifestPath `
        -Raw |
        ConvertFrom-Json

    $collectionsEntries = @(Get-ZipEntries -Path $collectionsComponentPath)

    Assert-Equal `
        -Expected "Mz.Collections" `
        -Actual ([string]$collectionsManifest.id) `
        -Message "Mz.Collections has the wrong package ID."

    Assert-Equal `
        -Expected $collectionsVersion `
        -Actual ([string]$collectionsManifest.version) `
        -Message "Mz.Collections has the wrong package version."

    Assert-Equal `
        -Expected $collectionsChangelogVersions `
        -Actual (@($collectionsManifest.changelog.version) -join ",") `
        -Message "Mz.Collections changelog order is incorrect."

    Assert-Equal `
        -Expected $semanticVersion `
        -Actual ([string]$collectionsManifest.dependencies."Mz.SemanticVersioning") `
        -Message "Mz.Collections has the wrong SemanticVersioning dependency."

    Assert-Equal `
        -Expected "Mz.Collections" `
        -Actual (@($collectionsManifest.folders | Sort-Object) -join ",") `
        -Message "Mz.Collections declares the wrong owned folders."

    Assert-True `
        -Condition (
            @(
                $collectionsEntries |
                    Where-Object {
                        -not $_.StartsWith(
                            "Libraries/Mz.Collections/",
                            [System.StringComparison]::Ordinal
                        )
                    }
            ).Count -eq 0
        ) `
        -Message "Mz.Collections archive contains files outside its owned folder."

    Assert-True `
        -Condition (
            @(
                $collectionsEntries |
                    Where-Object {
                        $_.StartsWith(
                            "Libraries/Mz.SemanticVersioning/",
                            [System.StringComparison]::Ordinal
                        )
                    }
            ).Count -eq 0
        ) `
        -Message "Mz.Collections component incorrectly embeds SemanticVersioning."

    Assert-True `
        -Condition (
            $collectionsEntries -contains
            "Libraries/Mz.Collections/ReadOnlyListView.cs"
        ) `
        -Message "Mz.Collections read-only list view is missing."

    Assert-True `
        -Condition (
            $collectionsEntries -contains
            "Libraries/Mz.Collections/ReadOnlyDictionaryView.cs"
        ) `
        -Message "Mz.Collections read-only dictionary view is missing."

    Assert-True `
        -Condition (
            $collectionsEntries -contains
            "Libraries/Mz.Collections/LibraryVersionFile.cs"
        ) `
        -Message "Mz.Collections release metadata is missing."

    Assert-True `
        -Condition (
            $collectionsEntries -contains
            "Libraries/Mz.Collections/README.md"
        ) `
        -Message "Mz.Collections README is missing."

    $apiOutput = Join-Path $testRoot "api"

    & $bundleScript `
        -Tag $apiTag `
        -OutputDirectory $apiOutput `
        -SkipTests |
        Out-Null

    $apiManifestPath = Join-Path `
        $apiOutput `
        ("Mz.ApiProtocol-" + $apiVersion + "-package.json")

    $apiComponentPath = Join-Path `
        $apiOutput `
        ("Mz.ApiProtocol-" + $apiVersion + "-component.zip")

    $apiManifest = Get-Content `
        -LiteralPath $apiManifestPath `
        -Raw |
        ConvertFrom-Json

    Assert-Equal `
        -Expected "Mz.ApiProtocol" `
        -Actual ([string]$apiManifest.id) `
        -Message "ApiProtocol has the wrong package ID."

    Assert-Equal `
        -Expected $apiVersion `
        -Actual ([string]$apiManifest.version) `
        -Message "ApiProtocol has the wrong package version."

    Assert-Equal `
        -Expected $apiChangelogVersions `
        -Actual (
            @($apiManifest.changelog.version) -join ","
        ) `
        -Message "ApiProtocol changelog order is incorrect."

    Assert-Equal `
        -Expected 2 `
        -Actual @(
            $apiManifest.dependencies.PSObject.Properties
        ).Count `
        -Message "ApiProtocol declares the wrong dependency count."

    Assert-Equal `
        -Expected $collectionsVersion `
        -Actual (
            [string]$apiManifest.dependencies."Mz.Collections"
        ) `
        -Message "ApiProtocol has the wrong Mz.Collections dependency."

    Assert-Equal `
        -Expected $semanticVersion `
        -Actual (
            [string]$apiManifest.dependencies."Mz.SemanticVersioning"
        ) `
        -Message "ApiProtocol has the wrong SemanticVersioning dependency."

    Assert-Equal `
        -Expected (
            "Mz.ApiProtocol.Core,Mz.ApiProtocol.SpaceEngineers"
        ) `
        -Actual (
            @($apiManifest.folders | Sort-Object) -join ","
        ) `
        -Message "ApiProtocol declares the wrong owned folders."

    $apiHash = (
        Get-FileHash `
            -LiteralPath $apiComponentPath `
            -Algorithm SHA256
    ).Hash.ToLowerInvariant()

    Assert-Equal `
        -Expected $apiHash `
        -Actual ([string]$apiManifest.component.sha256) `
        -Message "ApiProtocol manifest checksum is incorrect."

    $apiEntries = @(Get-ZipEntries -Path $apiComponentPath)

    Assert-True `
        -Condition (
            @(
                $apiEntries |
                    Where-Object {
                        -not $_.StartsWith(
                            "Libraries/",
                            [System.StringComparison]::Ordinal
                        )
                    }
            ).Count -eq 0
        ) `
        -Message "ApiProtocol archive has entries outside Libraries/."

    Assert-True `
        -Condition (
            @(
                $apiEntries |
                    Where-Object {
                        $_.StartsWith(
                            "Libraries/Mz.SemanticVersioning/",
                            [System.StringComparison]::Ordinal
                        )
                    }
            ).Count -eq 0
        ) `
        -Message (
            "ApiProtocol component incorrectly embeds its transitive " +
            "SemanticVersioning dependency."
        )

    Assert-True `
        -Condition (
            @(
                $apiEntries |
                    Where-Object {
                        $_.StartsWith(
                            "Libraries/Mz.Collections/",
                            [System.StringComparison]::Ordinal
                        )
                    }
            ).Count -eq 0
        ) `
        -Message "ApiProtocol component incorrectly embeds its Mz.Collections dependency."

    Assert-True `
        -Condition (
            @(
                $apiEntries |
                    Where-Object {
                        $_.StartsWith(
                            "Libraries/Mz.ApiProtocol.Core/",
                            [System.StringComparison]::Ordinal
                        )
                    }
            ).Count -gt 0
        ) `
        -Message "ApiProtocol Core sources are missing."

    Assert-True `
        -Condition (
            @(
                $apiEntries |
                    Where-Object {
                        $_.StartsWith(
                            "Libraries/Mz.ApiProtocol.SpaceEngineers/",
                            [System.StringComparison]::Ordinal
                        )
                    }
            ).Count -gt 0
        ) `
        -Message "ApiProtocol Space Engineers sources are missing."

    Assert-True `
        -Condition (
            $apiEntries -contains
            "Libraries/Mz.ApiProtocol.Core/README.md"
        ) `
        -Message "ApiProtocol README is missing from its archive."

    Assert-True `
        -Condition (
            $apiEntries -contains
            "Libraries/Mz.ApiProtocol.Core/Guide.md"
        ) `
        -Message "ApiProtocol copy-paste guide is missing from its archive."

    $loggingOutput = Join-Path $testRoot "logging"

    & $bundleScript `
        -Tag $loggingTag `
        -OutputDirectory $loggingOutput `
        -SkipTests |
        Out-Null

    $loggingManifest = Get-Content `
        -LiteralPath (
            Join-Path `
                $loggingOutput `
                ("Mz.Logging-" + $loggingVersion + "-package.json")
        ) `
        -Raw |
        ConvertFrom-Json

    $loggingEntries = @(
        Get-ZipEntries `
            -Path (
                Join-Path `
                    $loggingOutput `
                    ("Mz.Logging-" + $loggingVersion + "-component.zip")
            )
    )

    Assert-Equal `
        -Expected $semanticVersion `
        -Actual (
            [string]$loggingManifest.dependencies."Mz.SemanticVersioning"
        ) `
        -Message "Logging has the wrong SemanticVersioning dependency."

    Assert-True `
        -Condition (
            @(
                $loggingEntries |
                    Where-Object {
                        $_.StartsWith(
                            "Libraries/Mz.SemanticVersioning/",
                            [System.StringComparison]::Ordinal
                        )
                    }
            ).Count -eq 0
        ) `
        -Message (
            "Logging component incorrectly embeds its SemanticVersioning " +
            "dependency."
        )

    Assert-True `
        -Condition (
            $loggingEntries -contains
            "Libraries/Mz.Logging.Core/README.md"
        ) `
        -Message "Logging README is missing from its archive."

    $networkingOutput = Join-Path $testRoot "networking"

    & $bundleScript `
        -Tag $networkingTag `
        -OutputDirectory $networkingOutput `
        -SkipTests |
        Out-Null

    $networkingManifest = Get-Content `
        -LiteralPath (
            Join-Path `
                $networkingOutput `
                ("Mz.Networking-" + $networkingVersion + "-package.json")
        ) `
        -Raw |
        ConvertFrom-Json

    $networkingEntries = @(
        Get-ZipEntries `
            -Path (
                Join-Path `
                    $networkingOutput `
                    ("Mz.Networking-" + $networkingVersion + "-component.zip")
            )
    )

    Assert-Equal `
        -Expected $networkingVersion `
        -Actual ([string]$networkingManifest.version) `
        -Message "Networking has the wrong package version."

    Assert-Equal `
        -Expected $networkingChangelogVersions `
        -Actual (
            @($networkingManifest.changelog.version) -join ","
        ) `
        -Message "Networking changelog order is incorrect."

    Assert-Equal `
        -Expected 2 `
        -Actual @(
            $networkingManifest.dependencies.PSObject.Properties
        ).Count `
        -Message "Networking declares the wrong dependency count."

    Assert-Equal `
        -Expected $apiVersion `
        -Actual (
            [string]$networkingManifest.dependencies."Mz.ApiProtocol"
        ) `
        -Message "Networking has the wrong ApiProtocol dependency."

    Assert-Equal `
        -Expected $semanticVersion `
        -Actual (
            [string]$networkingManifest.dependencies."Mz.SemanticVersioning"
        ) `
        -Message "Networking has the wrong SemanticVersioning dependency."

    Assert-True `
        -Condition (
            @(
                $networkingEntries |
                    Where-Object {
                        $_.StartsWith(
                            "Libraries/Mz.SemanticVersioning/",
                            [System.StringComparison]::Ordinal
                        )
                    }
            ).Count -eq 0
        ) `
        -Message (
            "Networking component incorrectly embeds its SemanticVersioning " +
            "dependency."
        )

    Assert-True `
        -Condition (
            @(
                $networkingEntries |
                    Where-Object {
                        $_.StartsWith(
                            "Libraries/Mz.ApiProtocol.",
                            [System.StringComparison]::Ordinal
                        )
                    }
            ).Count -eq 0
        ) `
        -Message "Networking component incorrectly embeds its ApiProtocol dependency."

    Assert-True `
        -Condition (
            @(
                $networkingEntries |
                    Where-Object {
                        $_.StartsWith(
                            "Libraries/Mz.Collections/",
                            [System.StringComparison]::Ordinal
                        )
                    }
            ).Count -eq 0
        ) `
        -Message "Networking component incorrectly embeds transitive Mz.Collections sources."

    Assert-True `
        -Condition (
            $networkingEntries -contains
            "Libraries/Mz.Networking.Core/README.md"
        ) `
        -Message "Networking README is missing from its archive."

    Assert-True `
        -Condition (
            $networkingEntries -contains
            "Libraries/Mz.Networking.Core/Guide.md"
        ) `
        -Message "Networking copy-paste guide is missing from its archive."

    Assert-True `
        -Condition (
            $networkingEntries -contains (
                "Libraries/Mz.Networking.Core/" +
                "Endpoints/NetworkEndpoint.cs"
            )
        ) `
        -Message "Networking Core endpoint layout was not preserved."

    Assert-True `
        -Condition (
            $networkingEntries -contains (
                "Libraries/Mz.Networking.Core/" +
                "Dispatching/NetworkMessageDispatcher.cs"
            )
        ) `
        -Message "Networking Core dispatching layout was not preserved."

    Assert-True `
        -Condition (
            $networkingEntries -contains (
                "Libraries/Mz.Networking.SpaceEngineers/" +
                "Gateway/SpaceEngineersNetworkGateway.cs"
            )
        ) `
        -Message "Networking gateway layout was not preserved."

    Assert-True `
        -Condition (
            $networkingEntries -contains (
                "Libraries/Mz.Networking.SpaceEngineers/" +
                "Wire/NetworkEnvelopeWire.cs"
            )
        ) `
        -Message "Networking wire layout was not preserved."

    $storageOutput = Join-Path $testRoot "storage"

    & $bundleScript `
        -Tag $storageTag `
        -OutputDirectory $storageOutput `
        -SkipTests |
        Out-Null

    $storageManifestPath = Join-Path `
        $storageOutput `
        ("Mz.Storage-" + $storageVersion + "-package.json")

    $storageComponentPath = Join-Path `
        $storageOutput `
        ("Mz.Storage-" + $storageVersion + "-component.zip")

    $storageManifest = Get-Content `
        -LiteralPath $storageManifestPath `
        -Raw |
        ConvertFrom-Json

    $storageEntries = @(Get-ZipEntries -Path $storageComponentPath)

    Assert-Equal `
        -Expected "Mz.Storage" `
        -Actual ([string]$storageManifest.id) `
        -Message "Mz.Storage has the wrong package ID."

    Assert-Equal `
        -Expected $storageVersion `
        -Actual ([string]$storageManifest.version) `
        -Message "Mz.Storage has the wrong package version."

    Assert-Equal `
        -Expected $storageChangelogVersions `
        -Actual (@($storageManifest.changelog.version) -join ",") `
        -Message "Mz.Storage changelog order is incorrect."

    Assert-Equal `
        -Expected 1 `
        -Actual @($storageManifest.dependencies.PSObject.Properties).Count `
        -Message "Mz.Storage declares the wrong dependency count."

    Assert-Equal `
        -Expected $semanticVersion `
        -Actual ([string]$storageManifest.dependencies."Mz.SemanticVersioning") `
        -Message "Mz.Storage has the wrong SemanticVersioning dependency."

    Assert-Equal `
        -Expected "Mz.Storage.Core,Mz.Storage.SpaceEngineers" `
        -Actual (@($storageManifest.folders | Sort-Object) -join ",") `
        -Message "Mz.Storage declares the wrong owned folders."

    Assert-True `
        -Condition (
            @(
                $storageEntries |
                    Where-Object {
                        -not (
                            $_.StartsWith("Libraries/Mz.Storage.Core/", [System.StringComparison]::Ordinal) -or
                            $_.StartsWith("Libraries/Mz.Storage.SpaceEngineers/", [System.StringComparison]::Ordinal)
                        )
                    }
            ).Count -eq 0
        ) `
        -Message "Mz.Storage archive contains files outside its owned folders."

    Assert-True `
        -Condition (
            @(
                $storageEntries |
                    Where-Object {
                        $_.StartsWith("Libraries/Mz.SemanticVersioning/", [System.StringComparison]::Ordinal)
                    }
            ).Count -eq 0
        ) `
        -Message "Mz.Storage component incorrectly embeds SemanticVersioning."

    Assert-True `
        -Condition ($storageEntries -contains "Libraries/Mz.Storage.Core/LibraryVersionFile.cs") `
        -Message "Mz.Storage release metadata is missing."

    Assert-True `
        -Condition ($storageEntries -contains "Libraries/Mz.Storage.Core/README.md") `
        -Message "Mz.Storage README is missing."

    Assert-True `
        -Condition ($storageEntries -contains "Libraries/Mz.Storage.Core/IndexedStorage.cs") `
        -Message "Mz.Storage indexed core is missing."

    Assert-True `
        -Condition ($storageEntries -contains "Libraries/Mz.Storage.SpaceEngineers/SpaceEngineersStorage.cs") `
        -Message "Mz.Storage Space Engineers entry point is missing."

    Assert-True `
        -Condition (
            @(
                $storageEntries |
                    Where-Object {
                        $_ -match '\.csproj$' -or
                        $_ -match '/(?:bin|obj)/'
                    }
            ).Count -eq 0
        ) `
        -Message "Mz.Storage archive contains project or build artifacts."
    $tomlOutput = Join-Path $testRoot "toml"

    & $bundleScript `
        -Tag $tomlTag `
        -OutputDirectory $tomlOutput `
        -SkipTests |
        Out-Null

    $tomlManifestPath = Join-Path `
        $tomlOutput `
        ("Mz.Toml-" + $tomlVersion + "-package.json")

    $tomlComponentPath = Join-Path `
        $tomlOutput `
        ("Mz.Toml-" + $tomlVersion + "-component.zip")

    Assert-True `
        -Condition (
            Test-Path `
                -LiteralPath $tomlManifestPath `
                -PathType Leaf
        ) `
        -Message "Mz.Toml package manifest is missing."

    Assert-True `
        -Condition (
            Test-Path `
                -LiteralPath $tomlComponentPath `
                -PathType Leaf
        ) `
        -Message "Mz.Toml component archive is missing."

    $tomlManifest = Get-Content `
        -LiteralPath $tomlManifestPath `
        -Raw |
        ConvertFrom-Json

    $tomlEntries = @(
        Get-ZipEntries `
            -Path $tomlComponentPath
    )

    Assert-Equal `
        -Expected "Mz.Toml" `
        -Actual ([string]$tomlManifest.id) `
        -Message "Mz.Toml has the wrong package ID."

    Assert-Equal `
        -Expected $tomlVersion `
        -Actual ([string]$tomlManifest.version) `
        -Message "Mz.Toml has the wrong package version."

    Assert-Equal `
        -Expected $tomlChangelogVersions `
        -Actual (
            @($tomlManifest.changelog.version) -join ","
        ) `
        -Message "Mz.Toml changelog order is incorrect."

    Assert-Equal `
        -Expected 2 `
        -Actual @(
            $tomlManifest.dependencies.PSObject.Properties
        ).Count `
        -Message "Mz.Toml declares the wrong dependency count."

    Assert-Equal `
        -Expected $collectionsVersion `
        -Actual (
            [string]$tomlManifest.dependencies."Mz.Collections"
        ) `
        -Message "Mz.Toml has the wrong Mz.Collections dependency."

    Assert-Equal `
        -Expected $semanticVersion `
        -Actual (
            [string]$tomlManifest.dependencies."Mz.SemanticVersioning"
        ) `
        -Message "Mz.Toml has the wrong SemanticVersioning dependency."

    Assert-Equal `
        -Expected "Mz.Toml" `
        -Actual (
            @($tomlManifest.folders | Sort-Object) -join ","
        ) `
        -Message "Mz.Toml declares the wrong owned folders."

    $tomlHash = (
        Get-FileHash `
            -LiteralPath $tomlComponentPath `
            -Algorithm SHA256
    ).Hash.ToLowerInvariant()

    Assert-Equal `
        -Expected $tomlHash `
        -Actual ([string]$tomlManifest.component.sha256) `
        -Message "Mz.Toml manifest checksum is incorrect."

    Assert-True `
        -Condition (
            $tomlEntries.Count -gt 0
        ) `
        -Message "Mz.Toml archive is empty."

    Assert-True `
        -Condition (
            @(
                $tomlEntries |
                    Where-Object {
                        -not $_.StartsWith(
                            "Libraries/Mz.Toml/",
                            [System.StringComparison]::Ordinal
                        )
                    }
            ).Count -eq 0
        ) `
        -Message "Mz.Toml archive contains files outside its owned folder."

    Assert-True `
        -Condition (
            @(
                $tomlEntries |
                    Where-Object {
                        $_.StartsWith(
                            "Libraries/Mz.SemanticVersioning/",
                            [System.StringComparison]::Ordinal
                        )
                    }
            ).Count -eq 0
        ) `
        -Message (
            "Mz.Toml component incorrectly embeds its SemanticVersioning " +
            "dependency."
        )

    Assert-True `
        -Condition (
            @(
                $tomlEntries |
                    Where-Object {
                        $_.StartsWith(
                            "Libraries/Mz.Collections/",
                            [System.StringComparison]::Ordinal
                        )
                    }
            ).Count -eq 0
        ) `
        -Message "Mz.Toml component incorrectly embeds its Mz.Collections dependency."

    Assert-True `
        -Condition (
            $tomlEntries -contains
            "Libraries/Mz.Toml/README.md"
        ) `
        -Message "Mz.Toml README is missing from its archive."

    Assert-True `
        -Condition (
            $tomlEntries -contains
            "Libraries/Mz.Toml/Guide.md"
        ) `
        -Message "Mz.Toml copy-paste guide is missing from its archive."

    Assert-True `
        -Condition (
            $tomlEntries -contains
            "Libraries/Mz.Toml/LibraryVersionFile.cs"
        ) `
        -Message "Mz.Toml release metadata is missing from its archive."

    Assert-True `
        -Condition (
            $tomlEntries -contains
            "Libraries/Mz.Toml/Toml.cs"
        ) `
        -Message "Mz.Toml public entry point is missing from its archive."

    Assert-True `
        -Condition (
            $tomlEntries -contains
            "Libraries/Mz.Toml/Internal/TomlParser.cs"
        ) `
        -Message "Mz.Toml parser layout was not preserved."

    Assert-True `
        -Condition (
            $tomlEntries -contains
            "Libraries/Mz.Toml/Internal/TomlWriter.cs"
        ) `
        -Message "Mz.Toml writer layout was not preserved."

    Assert-True `
        -Condition (
            @(
                $tomlEntries |
                    Where-Object {
                        $_ -match '\.csproj$' -or
                        $_ -match '/(?:bin|obj)/'
                    }
            ).Count -eq 0
        ) `
        -Message "Mz.Toml archive contains project or build artifacts."

    Assert-Throws `
        -Action {
            & $bundleScript `
                -Tag "apiprotocol/v0.2.0-r2" `
                -OutputDirectory (Join-Path $testRoot "legacy") `
                -SkipTests |
                Out-Null
        } `
        -ExpectedMessagePart "Invalid release tag"

    Assert-Throws `
        -Action {
            & $bundleScript `
                -Tag "release/mz.Logging/0.1.1" `
                -OutputDirectory (Join-Path $testRoot "wrong-case") `
                -SkipTests |
                Out-Null
        } `
        -ExpectedMessagePart "was not discovered exactly once"

    Assert-Throws `
        -Action {
            & $bundleScript `
                -Tag "release/Mz.Unknown/1.0.0" `
                -OutputDirectory (Join-Path $testRoot "unknown") `
                -SkipTests |
                Out-Null
        } `
        -ExpectedMessagePart "was not discovered exactly once"

    Assert-Throws `
        -Action {
            & $bundleScript `
                -Tag "release/Mz.Logging/0.1.0" `
                -OutputDirectory (Join-Path $testRoot "wrong-version") `
                -SkipTests |
                Out-Null
        } `
        -ExpectedMessagePart "does not match LibraryVersionFile version"

    Write-Output (
        "OK SELibs package-format tests passed: " +
        "$script:Passed assertions"
    )
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item `
            -LiteralPath $testRoot `
            -Recurse `
            -Force
    }
}

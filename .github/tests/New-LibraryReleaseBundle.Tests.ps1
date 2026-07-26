$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Add-Type -AssemblyName System.IO.Compression.FileSystem

$repoRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..\..")
)

$bundleScript = Join-Path `
    $repoRoot `
    ".github\scripts\New-LibraryReleaseBundle.ps1"

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
    $semanticOutput = Join-Path $testRoot "semantic"

    & $bundleScript `
        -Tag "semanticversioning/v0.1.1" `
        -OutputDirectory $semanticOutput `
        -SkipTests |
        Out-Null

    $semanticManifestPath = Join-Path `
        $semanticOutput `
        "Mz.SemanticVersioning-0.1.1-package.json"

    $semanticComponentPath = Join-Path `
        $semanticOutput `
        "Mz.SemanticVersioning-0.1.1-component.zip"

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
        -Expected "0.1.1" `
        -Actual ([string]$semanticManifest.version) `
        -Message "SemanticVersioning has the wrong version."

    Assert-Equal `
        -Expected "0.1.1,0.1.0" `
        -Actual (
            @($semanticManifest.changelog.version) -join ","
        ) `
        -Message "SemanticVersioning changelog order is incorrect."

    Assert-Equal `
        -Expected (
            "Added a complete package usage guide and installation examples."
        ) `
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
                "- Added a complete package usage guide and installation examples."
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
            "Libraries/Mz.SemanticVersioning/README.md"
        ) `
        -Message "SemanticVersioning README is missing from its archive."

    $apiOutput = Join-Path $testRoot "api"

    & $bundleScript `
        -Tag "apiprotocol/v0.2.1" `
        -OutputDirectory $apiOutput `
        -SkipTests |
        Out-Null

    $apiManifestPath = Join-Path `
        $apiOutput `
        "Mz.ApiProtocol-0.2.1-package.json"

    $apiComponentPath = Join-Path `
        $apiOutput `
        "Mz.ApiProtocol-0.2.1-component.zip"

    $apiManifest = Get-Content `
        -LiteralPath $apiManifestPath `
        -Raw |
        ConvertFrom-Json

    Assert-Equal `
        -Expected "Mz.ApiProtocol" `
        -Actual ([string]$apiManifest.id) `
        -Message "ApiProtocol has the wrong package ID."

    Assert-Equal `
        -Expected "0.2.1" `
        -Actual ([string]$apiManifest.version) `
        -Message "ApiProtocol has the wrong package version."

    Assert-Equal `
        -Expected "0.2.1,0.2.0" `
        -Actual (
            @($apiManifest.changelog.version) -join ","
        ) `
        -Message "ApiProtocol changelog order is incorrect."

    Assert-Equal `
        -Expected "0.1.1" `
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

    $loggingOutput = Join-Path $testRoot "logging"

    & $bundleScript `
        -Tag "logging/v0.1.1" `
        -OutputDirectory $loggingOutput `
        -SkipTests |
        Out-Null

    $loggingEntries = @(
        Get-ZipEntries `
            -Path (
                Join-Path `
                    $loggingOutput `
                    "Mz.Logging-0.1.1-component.zip"
            )
    )

    Assert-True `
        -Condition (
            $loggingEntries -contains
            "Libraries/Mz.Logging.Core/README.md"
        ) `
        -Message "Logging README is missing from its archive."

    $networkingOutput = Join-Path $testRoot "networking"

    & $bundleScript `
        -Tag "networking/v0.1.1" `
        -OutputDirectory $networkingOutput `
        -SkipTests |
        Out-Null

    $networkingEntries = @(
        Get-ZipEntries `
            -Path (
                Join-Path `
                    $networkingOutput `
                    "Mz.Networking-0.1.1-component.zip"
            )
    )

    Assert-True `
        -Condition (
            $networkingEntries -contains
            "Libraries/Mz.Networking.Core/README.md"
        ) `
        -Message "Networking README is missing from its archive."

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

    Assert-Throws `
        -Action {
            & $bundleScript `
                -Tag "apiprotocol/v0.2.0-r2" `
                -OutputDirectory (Join-Path $testRoot "invalid") `
                -SkipTests |
                Out-Null
        } `
        -ExpectedMessagePart "Invalid release tag"

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

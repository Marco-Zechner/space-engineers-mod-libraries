$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..\..")
)

$generator = Join-Path `
    $repoRoot `
    ".github\scripts\New-RepositoryReadme.ps1"

$script:Passed = 0
$encoding = New-Object System.Text.UTF8Encoding($false)

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

function Write-TestJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [object]$Value
    )

    $parent = Split-Path -Parent $Path

    New-Item `
        -ItemType Directory `
        -Path $parent `
        -Force |
        Out-Null

    [System.IO.File]::WriteAllText(
        $Path,
        (($Value | ConvertTo-Json -Depth 12) + "`n"),
        $encoding
    )
}

function New-TestManifest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,

        [Parameter(Mandatory = $true)]
        [string]$Id,

        [Parameter(Mandatory = $true)]
        [string]$Version,

        [string[]]$Changes,

        [object[]]$History
    )

    $path = Join-Path $Root "$Id-$Version-package.json"

    $manifest = [ordered]@{
        schemaVersion = 1
        id = $Id
        version = $Version
        dependencies = [ordered]@{}
        folders = @($Id)
        component = [ordered]@{
            asset = "$Id-$Version-component.zip"
            sha256 = ("0" * 64)
        }
    }

    if ($null -ne $History) {
        $manifest["changelog"] = @($History)
    }
    elseif ($null -ne $Changes) {
        $manifest["changelog"] = @(
            [ordered]@{
                version = $Version
                changes = @($Changes)
            }
        )
    }

    Write-TestJson -Path $path -Value $manifest
    return $path
}

function New-TestRelease {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Tag,

        [Parameter(Mandatory = $true)]
        [string]$ManifestPath,

        [switch]$Draft,

        [switch]$Prerelease
    )

    return [ordered]@{
        tag_name = $Tag
        html_url = "https://example.invalid/releases/$Tag"
        draft = [bool]$Draft
        prerelease = [bool]$Prerelease
        assets = @(
            [ordered]@{
                name = Split-Path -Leaf $ManifestPath
                localPath = $ManifestPath
            }
        )
    }
}

$testRoot = Join-Path `
    ([System.IO.Path]::GetTempPath()) `
    ("repository-readme-" + [Guid]::NewGuid().ToString("N"))

New-Item -ItemType Directory -Path $testRoot | Out-Null

try {
    $manifests = Join-Path $testRoot "manifests"

    $apiOld = New-TestManifest `
        -Root $manifests `
        -Id "Mz.ApiProtocol" `
        -Version "0.2.0" `
        -Changes @("Published the old API release.")

    $apiLatest = New-TestManifest `
        -Root $manifests `
        -Id "Mz.ApiProtocol" `
        -Version "0.2.1" `
        -History @(
            [ordered]@{
                version = "0.2.1"
                changes = @(
                    "Added API usage guides."
                    "Improved package metadata."
                )
            }
            [ordered]@{
                version = "0.2.0"
                changes = @(
                    "Published the old API release."
                )
            }
        )

    $logging = New-TestManifest `
        -Root $manifests `
        -Id "Mz.Logging" `
        -Version "0.1.1"

    $networking = New-TestManifest `
        -Root $manifests `
        -Id "Mz.Networking" `
        -Version "0.1.1" `
        -Changes @("Organized networking sources.")

    $semantic = New-TestManifest `
        -Root $manifests `
        -Id "Mz.SemanticVersioning" `
        -Version "0.1.1" `
        -Changes @("Added semantic-version documentation.")

    $draftManifest = New-TestManifest `
        -Root $manifests `
        -Id "Mz.ApiProtocol" `
        -Version "9.0.0" `
        -Changes @("This draft must not be selected.")

    $releaseMetadataPath = Join-Path $testRoot "releases.json"

    Write-TestJson `
        -Path $releaseMetadataPath `
        -Value @(
            (New-TestRelease `
                -Tag "apiprotocol/v0.2.0" `
                -ManifestPath $apiOld)
            (New-TestRelease `
                -Tag "apiprotocol/v0.2.1" `
                -ManifestPath $apiLatest)
            (New-TestRelease `
                -Tag "logging/v0.1.1" `
                -ManifestPath $logging)
            (New-TestRelease `
                -Tag "networking/v0.1.1" `
                -ManifestPath $networking)
            (New-TestRelease `
                -Tag "semanticversioning/v0.1.1" `
                -ManifestPath $semantic)
            (New-TestRelease `
                -Tag "apiprotocol/v9.0.0" `
                -ManifestPath $draftManifest `
                -Draft)
        )

    $outputPath = Join-Path $testRoot "README.md"

    & $generator `
        -Repository "Example/Repository" `
        -OutputPath $outputPath `
        -ReleaseMetadataPath $releaseMetadataPath |
        Out-Null

    $text = Get-Content -LiteralPath $outputPath -Raw

    Assert-True `
        -Condition (
            $text.Contains("# Space Engineers Mod Libraries")
        ) `
        -Message "Generated README has the wrong title."

    Assert-True `
        -Condition (
            $text.Contains(
                "[``0.2.1``]" +
                "(https://example.invalid/releases/apiprotocol/v0.2.1)"
            )
        ) `
        -Message "Generated README did not select the newest API release."

    Assert-True `
        -Condition (-not $text.Contains("apiprotocol/v9.0.0")) `
        -Message "Generated README selected a draft release."

    Assert-True `
        -Condition ($text.Contains("- Added API usage guides.")) `
        -Message "Generated README omitted the newest changelog."

    Assert-True `
        -Condition (
            -not $text.Contains("- Published the old API release.")
        ) `
        -Message "Generated README printed historical instead of latest changes."

    Assert-True `
        -Condition (
            $text.Contains(
                "- No changelog metadata was published for this release."
            )
        ) `
        -Message "Generated README omitted the legacy-release fallback."

    Assert-True `
        -Condition (
            $text.Contains(
                "[Guide](src/Mz.Networking.Core/README.md)"
            )
        ) `
        -Message "Generated README omitted package documentation links."

    Assert-True `
        -Condition (
            $text.Contains(
                "[SELibs](https://github.com/Marco-Zechner/selibs)"
            )
        ) `
        -Message "Generated README omitted the SELibs link."

    Write-Output (
        "OK repository README tests passed: " +
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

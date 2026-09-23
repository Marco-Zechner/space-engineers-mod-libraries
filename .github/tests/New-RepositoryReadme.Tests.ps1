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

        [switch]$Prerelease,

        [switch]$EmptyEmbeddedAssets,

        [string]$AssetMetadataPath
    )

    $assets = @()

    if (-not $EmptyEmbeddedAssets) {
        $assets = @(
            [ordered]@{
                name = Split-Path -Leaf $ManifestPath
                localPath = $ManifestPath
            }
        )
    }

    return [ordered]@{
        tag_name = $Tag
        html_url = "https://example.invalid/releases/$Tag"
        draft = [bool]$Draft
        prerelease = [bool]$Prerelease
        assets = $assets
        assetMetadataPath = $AssetMetadataPath
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

    $storage = New-TestManifest `
        -Root $manifests `
        -Id "Mz.Storage" `
        -Version "0.1.1" `
        -Changes @("Normalized Global metadata filenames.")

    $storageAssetMetadataPath = Join-Path $testRoot "storage-assets.json"

    Write-TestJson `
        -Path $storageAssetMetadataPath `
        -Value @(
            [ordered]@{
                name = Split-Path -Leaf $storage
                localPath = $storage
            }
        )

    $releaseMetadataPath = Join-Path $testRoot "releases.json"

    Write-TestJson `
        -Path $releaseMetadataPath `
        -Value @(
            (New-TestRelease `
                -Tag "release/Mz.ApiProtocol/0.2.0" `
                -ManifestPath $apiOld)
            (New-TestRelease `
                -Tag "release/Mz.ApiProtocol/0.2.1" `
                -ManifestPath $apiLatest)
            (New-TestRelease `
                -Tag "release/Mz.Logging/0.1.1" `
                -ManifestPath $logging)
            (New-TestRelease `
                -Tag "release/Mz.Networking/0.1.1" `
                -ManifestPath $networking)
            (New-TestRelease `
                -Tag "release/Mz.SemanticVersioning/0.1.1" `
                -ManifestPath $semantic)
            (New-TestRelease `
                -Tag "release/Mz.Storage/0.1.1" `
                -ManifestPath $storage `
                -EmptyEmbeddedAssets `
                -AssetMetadataPath $storageAssetMetadataPath)
            (New-TestRelease `
                -Tag "release/Mz.ApiProtocol/9.0.0" `
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
                "[``0.1.1``]" +
                "(https://example.invalid/releases/release/Mz.Storage/0.1.1)"
            )
        ) `
        -Message (
            "Generated README did not resolve release assets from " +
            "authoritative asset metadata."
        )

    Assert-True `
        -Condition (
            $text.Contains(
                "[``0.2.1``]" +
                "(https://example.invalid/releases/release/Mz.ApiProtocol/0.2.1)"
            )
        ) `
        -Message "Generated README did not select the newest API release."

    Assert-True `
        -Condition (-not $text.Contains("release/Mz.ApiProtocol/9.0.0")) `
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

    $releaseWorkflowText = Get-Content `
        -LiteralPath (
            Join-Path `
                $repoRoot `
                ".github\workflows\releases.yml"
        ) `
        -Raw

    $readmeWorkflowText = Get-Content `
        -LiteralPath (
            Join-Path `
                $repoRoot `
                ".github\workflows\update-readme.yml"
        ) `
        -Raw

    Assert-True `
        -Condition (
            $releaseWorkflowText.Contains(
                "uses: ./.github/workflows/update-readme.yml"
            )
        ) `
        -Message (
            "Automated tag releases do not invoke the README workflow after " +
            "creating the GitHub release."
        )

    Assert-True `
        -Condition (
            $releaseWorkflowText.Contains("needs: release")
        ) `
        -Message "README generation is not ordered after release publication."

    Assert-True `
        -Condition (
            $releaseWorkflowText.Contains(
                '- "release/*/*"'
            )
        ) `
        -Message (
            "Release workflow does not select namespace/version tags."
        )

    Assert-True `
        -Condition (
            $readmeWorkflowText.Contains(
                'git diff --cached --quiet'
            ) `
            -and $readmeWorkflowText.Contains(
                '$diffExitCode = $LASTEXITCODE'
            ) `
            -and $readmeWorkflowText.Contains(
                'if ($diffExitCode -eq 0)'
            )
        ) `
        -Message (
            "README workflow does not handle an unchanged generated " +
            "README through the Git exit code."
        )

    Assert-True `
        -Condition (
            -not $readmeWorkflowText.Contains(
                'if (git diff --cached --quiet)'
            )
        ) `
        -Message (
            "README workflow still treats native command output as a " +
            "PowerShell Boolean."
        )

    Assert-True `
        -Condition (
            $readmeWorkflowText.Contains("workflow_call:")
        ) `
        -Message "README workflow is not reusable."

    Assert-True `
        -Condition (
            $readmeWorkflowText.Contains("- published")
        ) `
        -Message (
            "README workflow no longer handles manual release publication."
        )

    Assert-True `
        -Condition (
            -not $readmeWorkflowText.Contains(
                "group: repository-readme"
            )
        ) `
        -Message (
            "README updates still use a coalescing concurrency group that " +
            "cancels pending burst-release updates."
        )

    Assert-True `
        -Condition (
            $readmeWorkflowText.Contains(
                'for ($attempt = 1; $attempt -le 5; $attempt++)'
            ) `
            -and $readmeWorkflowText.Contains(
                "git fetch origin main"
            ) `
            -and $readmeWorkflowText.Contains(
                "git reset --hard origin/main"
            ) `
            -and $readmeWorkflowText.Contains(
                "main advanced while publishing README.md"
            )
        ) `
        -Message (
            "README publication does not retry against the latest main when " +
            "parallel release updates race."
        )
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

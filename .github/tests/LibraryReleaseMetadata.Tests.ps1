$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..\..")
)

$metadataScript = Join-Path `
    $repoRoot `
    ".github\scripts\LibraryReleaseMetadata.ps1"

. $metadataScript

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

function Write-TestFileLines {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string[]]$Lines
    )

    $parent = Split-Path -Parent $Path

    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item `
            -ItemType Directory `
            -Path $parent `
            -Force |
            Out-Null
    }

    $encoding = New-Object System.Text.UTF8Encoding($false)

    [System.IO.File]::WriteAllText(
        $Path,
        (($Lines -join "`n") + "`n"),
        $encoding
    )
}

$testRoot = Join-Path `
    ([System.IO.Path]::GetTempPath()) `
    ("library-release-metadata-" + [Guid]::NewGuid().ToString("N"))

try {
    Write-TestFileLines `
        -Path (
            Join-Path `
                $testRoot `
                "src\Test.Dependency\LibraryVersionFile.cs"
        ) `
        -Lines @(
            "namespace Test.Dependency",
            "{",
            "    public static class LibraryVersionFile",
            "    {",
            "        public const int Major = 2;",
            "        public const int Minor = 3;",
            "        public const int Patch = 4;",
            "    }",
            "}"
        )

    Write-TestFileLines `
        -Path (
            Join-Path `
                $testRoot `
                "src\Test.Dependency\Test.Dependency.csproj"
        ) `
        -Lines @(
            '<Project Sdk="Microsoft.NET.Sdk">',
            "  <PropertyGroup>",
            "    <TargetFramework>net8.0</TargetFramework>",
            "    <RootNamespace>Test.Dependency</RootNamespace>",
            "  </PropertyGroup>",
            "</Project>"
        )

    Write-TestFileLines `
        -Path (
            Join-Path `
                $testRoot `
                "src\Test.Dependency\Dependency.cs"
        ) `
        -Lines @(
            "namespace Test.Dependency",
            "{",
            "    public sealed class Dependency",
            "    {",
            "    }",
            "}"
        )

    Write-TestFileLines `
        -Path (
            Join-Path `
                $testRoot `
                "src\Test.Root.Core\LibraryVersionFile.cs"
        ) `
        -Lines @(
            "namespace Test.Root",
            "{",
            "    public static class LibraryVersionFile",
            "    {",
            "        public const int Major = 1;",
            "        public const int Minor = 0;",
            "        public const int Patch = 0;",
            "    }",
            "}"
        )

    Write-TestFileLines `
        -Path (
            Join-Path `
                $testRoot `
                "src\Test.Root.Core\Test.Root.Core.csproj"
        ) `
        -Lines @(
            '<Project Sdk="Microsoft.NET.Sdk">',
            "  <PropertyGroup>",
            "    <TargetFramework>net8.0</TargetFramework>",
            "    <RootNamespace>Test.Root</RootNamespace>",
            "  </PropertyGroup>",
            "  <ItemGroup>",
            "    <ProjectReference",
            '      Include="..\Test.Dependency\Test.Dependency.csproj" />',
            "    <Compile",
            '      Include="..\..\Libraries\External.Dependency.Core\**\*.cs" />',
            "  </ItemGroup>",
            "</Project>"
        )

    Write-TestFileLines `
        -Path (
            Join-Path `
                $testRoot `
                "src\Test.Root.Core\Root.cs"
        ) `
        -Lines @(
            "namespace Test.Root",
            "{",
            "    public sealed class Root",
            "    {",
            "    }",
            "}"
        )

    Write-TestFileLines `
        -Path (
            Join-Path `
                $testRoot `
                "src\Test.Root.Adapter\Test.Root.Adapter.csproj"
        ) `
        -Lines @(
            '<Project Sdk="Microsoft.NET.Sdk">',
            "  <PropertyGroup>",
            "    <TargetFramework>net8.0</TargetFramework>",
            "    <RootNamespace>Test.Root.Adapter</RootNamespace>",
            "  </PropertyGroup>",
            "  <ItemGroup>",
            "    <ProjectReference",
            '      Include="..\Test.Root.Core\Test.Root.Core.csproj" />',
            "  </ItemGroup>",
            "</Project>"
        )

    Write-TestFileLines `
        -Path (
            Join-Path `
                $testRoot `
                "src\Test.Root.Adapter\Adapter.cs"
        ) `
        -Lines @(
            "namespace Test.Root.Adapter",
            "{",
            "    public sealed class Adapter",
            "    {",
            "    }",
            "}"
        )

    Write-TestFileLines `
        -Path (
            Join-Path `
                $testRoot `
                "Libraries\External.Dependency.Core\External.cs"
        ) `
        -Lines @(
            "namespace External.Dependency",
            "{",
            "    public sealed class External",
            "    {",
            "    }",
            "}"
        )

    Write-TestFileLines `
        -Path (Join-Path $testRoot "selibs.json") `
        -Lines @(
            "{",
            '  "schemaVersion": 1,',
            '  "librariesPath": "Libraries",',
            '  "dependencies": {}',
            "}"
        )

    Write-TestFileLines `
        -Path (Join-Path $testRoot "selibs.lock.json") `
        -Lines @(
            "{",
            '  "schemaVersion": 1,',
            '  "packages": {',
            '    "External.Dependency": {',
            '      "version": "4.5.6",',
            '      "direct": false,',
            '      "source": {},',
            '      "dependencies": {},',
            '      "folders": [',
            '        "External.Dependency.Core"',
            "      ],",
            '      "files": {}',
            "    }",
            "  }",
            "}"
        )

    Write-TestFileLines `
        -Path (
            Join-Path `
                $testRoot `
                "tests\OddPortableCheck\OddPortableCheck.csproj"
        ) `
        -Lines @(
            '<Project Sdk="Microsoft.NET.Sdk">',
            "  <PropertyGroup>",
            "    <TargetFramework>net8.0</TargetFramework>",
            "  </PropertyGroup>",
            "  <ItemGroup>",
            "    <ProjectReference",
            '      Include="..\..\src\Test.Root.Core\Test.Root.Core.csproj" />',
            "  </ItemGroup>",
            "</Project>"
        )

    Write-TestFileLines `
        -Path (
            Join-Path `
                $testRoot `
                "tests\AdapterOnlyCheck\AdapterOnlyCheck.csproj"
        ) `
        -Lines @(
            '<Project Sdk="Microsoft.NET.Sdk">',
            "  <PropertyGroup>",
            "    <TargetFramework>net8.0</TargetFramework>",
            "  </PropertyGroup>",
            "  <ItemGroup>",
            "    <ProjectReference",
            '      Include="..\..\src\Test.Root.Adapter\Test.Root.Adapter.csproj" />',
            "  </ItemGroup>",
            "</Project>"
        )
    $libraries = @(
        Get-ReleaseLibraries -RepoRoot $testRoot
    )

    Assert-Equal `
        -Expected 2 `
        -Actual $libraries.Count `
        -Message "The fixture did not discover exactly two packages."

    $rootPackage = @(
        $libraries |
            Where-Object { $_.PackageId -eq "Test.Root" }
    )[0]

    $dependencyPackage = @(
        $libraries |
            Where-Object { $_.PackageId -eq "Test.Dependency" }
    )[0]

    Assert-Equal `
        -Expected "root" `
        -Actual ([string]$rootPackage.Slug) `
        -Message "Package slug was not derived from the namespace."

    Assert-Equal `
        -Expected "1.0.0" `
        -Actual ([string]$rootPackage.Version) `
        -Message "Root package version was not read correctly."

    Assert-Equal `
        -Expected "2.3.4" `
        -Actual ([string]$dependencyPackage.Version) `
        -Message "Dependency package version was not read correctly."

    Assert-Equal `
        -Expected "src/Test.Root.Adapter,src/Test.Root.Core" `
        -Actual (
            @($rootPackage.SourceDirectories | Sort-Object) -join ","
        ) `
        -Message "Adapter project membership was not inferred."
    $portableTests = @(
        Get-LibraryPortableTestProjects `
            -Library $rootPackage `
            -RepoRoot $testRoot
    )

    Assert-Equal `
        -Expected 1 `
        -Actual $portableTests.Count `
        -Message "Portable tests were not selected by root-project reference."

    Assert-Equal `
        -Expected "OddPortableCheck" `
        -Actual ([string]$portableTests[0].BaseName) `
        -Message "Portable test discovery still depends on project naming."

    $dependencies = Resolve-LibraryDependencies `
        -Library $rootPackage `
        -Libraries $libraries `
        -RepoRoot $testRoot

    Assert-Equal `
        -Expected "2.3.4" `
        -Actual ([string]$dependencies["Test.Dependency"]) `
        -Message "ProjectReference dependency was not inferred."

    Assert-Equal `
        -Expected "4.5.6" `
        -Actual ([string]$dependencies["External.Dependency"]) `
        -Message "Locked source dependency was not inferred."

    Assert-Equal `
        -Expected 2 `
        -Actual $dependencies.Count `
        -Message "The package declared an unexpected dependency."

    $lock = Get-Content `
        -LiteralPath (Join-Path $testRoot "selibs.lock.json") `
        -Raw |
        ConvertFrom-Json

    $lock.packages.PSObject.Properties.Remove(
        "External.Dependency"
    )

    $encoding = New-Object System.Text.UTF8Encoding($false)

    [System.IO.File]::WriteAllText(
        (Join-Path $testRoot "selibs.lock.json"),
        (($lock | ConvertTo-Json -Depth 20) + "`n"),
        $encoding
    )

    Assert-Throws `
        -Action {
            Resolve-LibraryDependencies `
                -Library $rootPackage `
                -Libraries $libraries `
                -RepoRoot $testRoot |
                Out-Null
        } `
        -ExpectedMessagePart "not owned by any package"

    Write-Output (
        "OK library release metadata tests passed: " +
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

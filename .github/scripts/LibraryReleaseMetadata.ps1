Set-StrictMode -Version Latest

function Test-LibraryPathInsideRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)

    $fullRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar
    )

    if (
        $fullPath.Equals(
            $fullRoot,
            [System.StringComparison]::OrdinalIgnoreCase
        )
    ) {
        return $true
    }

    return $fullPath.StartsWith(
        $fullRoot + [System.IO.Path]::DirectorySeparatorChar,
        [System.StringComparison]::OrdinalIgnoreCase
    )
}

function Get-LibraryObjectPropertyValue {
    param(
        [AllowNull()]
        [object]$Object,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if ($null -eq $Object) {
        return $null
    }

    $property = $Object.PSObject.Properties[$Name]

    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}
function ConvertFrom-LibraryCSharpStringLiteral {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    try {
        return [System.Text.RegularExpressions.Regex]::Unescape(
            $Value
        )
    }
    catch {
        throw (
            "Library changelog in '$Path' contains an unsupported " +
            "C# string escape."
        )
    }
}

function Read-LibraryVersionDescriptor {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Library version file not found: $Path"
    }

    $text = Get-Content -LiteralPath $Path -Raw

    $namespaceMatch = [regex]::Match(
        $text,
        (
            '(?m)^\s*namespace\s+' +
            '(?<value>[A-Za-z_][A-Za-z0-9_.]*)\s*(?:\{|$)'
        )
    )

    if (-not $namespaceMatch.Success) {
        throw (
            "Library version file '$Path' does not declare a supported " +
            "namespace."
        )
    }

    $packageId = $namespaceMatch.Groups["value"].Value
    $versionParts = [ordered]@{}

    foreach ($name in @("Major", "Minor", "Patch")) {
        $match = [regex]::Match(
            $text,
            (
                'public\s+const\s+int\s+' +
                [regex]::Escape($name) +
                '\s*=\s*(?<value>[0-9]+)\s*;'
            )
        )

        if (-not $match.Success) {
            throw (
                "Library version file '$Path' does not declare " +
                "numeric $name."
            )
        }

        $versionParts[$name] = $match.Groups["value"].Value
    }

    $version = (
        "$($versionParts["Major"])." +
        "$($versionParts["Minor"])." +
        "$($versionParts["Patch"])"
    )

    $entryPattern = (
        '(?s)new\s+ChangelogEntry\s*\(\s*' +
        '"(?<version>(?:\\.|[^"\\])*)"\s*,\s*' +
        'new\s*\[\]\s*\{(?<changes>.*?)\}\s*\)'
    )

    $entryMatches = @(
        [regex]::Matches(
            $text,
            $entryPattern
        )
    )

    if ($entryMatches.Count -eq 0) {
        throw (
            "Library version file '$Path' does not declare any " +
            "ChangelogEntry values."
        )
    }

    $changelog = New-Object System.Collections.ArrayList
    $versions = @{}
    $previousVersion = $null

    foreach ($entryMatch in $entryMatches) {
        $entryVersion = ConvertFrom-LibraryCSharpStringLiteral `
            -Value $entryMatch.Groups["version"].Value `
            -Path $Path

        if ($entryVersion -notmatch '^[0-9]+\.[0-9]+\.[0-9]+$') {
            throw (
                "Library changelog in '$Path' contains invalid version " +
                "'$entryVersion'."
            )
        }

        $versionKey = $entryVersion.ToLowerInvariant()

        if ($versions.ContainsKey($versionKey)) {
            throw (
                "Library changelog in '$Path' declares version " +
                "'$entryVersion' more than once."
            )
        }

        $parsedVersion = [version]::Parse($entryVersion)

        if (
            $null -ne $previousVersion -and
            $parsedVersion.CompareTo($previousVersion) -ge 0
        ) {
            throw (
                "Library changelog in '$Path' must be ordered from " +
                "newest to oldest."
            )
        }

        $changesText = $entryMatch.Groups["changes"].Value
        $changeMatches = @(
            [regex]::Matches(
                $changesText,
                '"(?<value>(?:\\.|[^"\\])*)"'
            )
        )

        if ($changeMatches.Count -eq 0) {
            throw (
                "Library changelog version '$entryVersion' in '$Path' " +
                "does not contain any changes."
            )
        }

        $residue = [regex]::Replace(
            $changesText,
            '"(?:\\.|[^"\\])*"',
            ""
        )

        $residue = [regex]::Replace(
            $residue,
            '[\s,]',
            ""
        )

        if (-not [string]::IsNullOrEmpty($residue)) {
            throw (
                "Library changelog version '$entryVersion' in '$Path' " +
                "uses unsupported change-list syntax."
            )
        }

        $changes = New-Object System.Collections.ArrayList

        foreach ($changeMatch in $changeMatches) {
            $change = ConvertFrom-LibraryCSharpStringLiteral `
                -Value $changeMatch.Groups["value"].Value `
                -Path $Path

            if ([string]::IsNullOrWhiteSpace($change)) {
                throw (
                    "Library changelog version '$entryVersion' in '$Path' " +
                    "contains an empty change."
                )
            }

            [void]$changes.Add($change)
        }

        [void]$changelog.Add(
            [pscustomobject]@{
                Version = $entryVersion
                Changes = @($changes)
            }
        )

        $versions[$versionKey] = $true
        $previousVersion = $parsedVersion
    }

    if ([string]$changelog[0].Version -ne $version) {
        throw (
            "Library changelog in '$Path' must begin with current " +
            "package version '$version'."
        )
    }

    $packageLeaf = $packageId.Split(".")[-1]
    $slug = $packageLeaf.ToLowerInvariant().Replace("_", "-")

    if ($slug -notmatch '^[a-z0-9][a-z0-9-]*$') {
        throw (
            "Package namespace '$packageId' cannot produce a valid " +
            "release slug."
        )
    }

    return [pscustomobject]@{
        PackageId = $packageId
        Slug = $slug
        Version = $version
        Changelog = @($changelog)
        VersionFilePath = [System.IO.Path]::GetFullPath($Path)
    }
}
function Get-LibraryProjectTargetFramework {
    param(
        [Parameter(Mandatory = $true)]
        [xml]$Project,

        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $targetFrameworksNodes = @(
        $Project.SelectNodes(
            (
                "/*[local-name()='Project']" +
                "/*[local-name()='PropertyGroup']" +
                "/*[local-name()='TargetFrameworks']"
            )
        ) |
            Where-Object {
                -not [string]::IsNullOrWhiteSpace(
                    [string]$_.InnerText
                )
            }
    )

    if ($targetFrameworksNodes.Count -gt 0) {
        $frameworks = @(
            ([string]$targetFrameworksNodes[0].InnerText).Split(
                ";",
                [System.StringSplitOptions]::RemoveEmptyEntries
            )
        )

        if ($frameworks.Count -gt 0) {
            return [string]$frameworks[0]
        }
    }

    $targetFrameworkNodes = @(
        $Project.SelectNodes(
            (
                "/*[local-name()='Project']" +
                "/*[local-name()='PropertyGroup']" +
                "/*[local-name()='TargetFramework']"
            )
        ) |
            Where-Object {
                -not [string]::IsNullOrWhiteSpace(
                    [string]$_.InnerText
                )
            }
    )

    if ($targetFrameworkNodes.Count -gt 0) {
        return [string]$targetFrameworkNodes[0].InnerText
    }

    throw "Project '$ProjectPath' does not declare a target framework."
}
function ConvertFrom-LibraryMsBuildJson {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Output,

        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $text = $Output -join [Environment]::NewLine
    $start = $text.IndexOf("{")
    $end = $text.LastIndexOf("}")

    if ($start -lt 0 -or $end -lt $start) {
        throw "MSBuild returned no JSON for '$ProjectPath'."
    }

    try {
        return $text.Substring(
            $start,
            $end - $start + 1
        ) | ConvertFrom-Json
    }
    catch {
        throw (
            "MSBuild returned invalid JSON for '$ProjectPath': " +
            "$($_.Exception.Message)"
        )
    }
}

function Get-LibraryEvaluatedProjectItems {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$TargetFramework
    )

    $output = @(
        & dotnet msbuild `
            $ProjectPath `
            -nologo `
            -verbosity:quiet `
            "-property:TargetFramework=$TargetFramework" `
            -getItem:Compile `
            -getItem:ProjectReference
    )

    if ($LASTEXITCODE -ne 0) {
        $output | Select-Object -First 80
        throw "MSBuild item query failed for '$ProjectPath'."
    }

    $result = ConvertFrom-LibraryMsBuildJson `
        -Output $output `
        -ProjectPath $ProjectPath

    $compileItems = @()
    $projectReferences = @()

    $items = Get-LibraryObjectPropertyValue `
        -Object $result `
        -Name "Items"

    if ($null -eq $items) {
        throw "MSBuild item JSON for '$ProjectPath' does not define Items."
    }

    $compileValue = Get-LibraryObjectPropertyValue `
        -Object $items `
        -Name "Compile"

    if ($null -ne $compileValue) {
        $compileItems = @($compileValue)
    }

    $referenceValue = Get-LibraryObjectPropertyValue `
        -Object $items `
        -Name "ProjectReference"

    if ($null -ne $referenceValue) {
        $projectReferences = @($referenceValue)
    }

    return [pscustomobject]@{
        Compile = $compileItems
        ProjectReference = $projectReferences
    }
}

function Get-LibrarySourceProjectInfos {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $sourceRoot = Join-Path $RepoRoot "src"

    if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
        throw "Source root not found: $sourceRoot"
    }

    $result = New-Object System.Collections.ArrayList

    foreach (
        $projectFile in @(
            Get-ChildItem `
                -LiteralPath $sourceRoot `
                -Recurse `
                -File `
                -Filter "*.csproj" |
                Where-Object {
                    $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
                } |
                Sort-Object FullName
        )
    ) {
        try {
            [xml]$project = Get-Content `
                -LiteralPath $projectFile.FullName `
                -Raw
        }
        catch {
            throw (
                "Could not read project '$($projectFile.FullName)': " +
                "$($_.Exception.Message)"
            )
        }

        $targetFramework = Get-LibraryProjectTargetFramework `
            -Project $project `
            -ProjectPath $projectFile.FullName

        $rootNamespaceNodes = @(
            $project.SelectNodes(
                (
                    "/*[local-name()='Project']" +
                    "/*[local-name()='PropertyGroup']" +
                    "/*[local-name()='RootNamespace']"
                )
            ) |
                Where-Object {
                    -not [string]::IsNullOrWhiteSpace(
                        [string]$_.InnerText
                    )
                }
        )

        if ($rootNamespaceNodes.Count -gt 0) {
            $rootNamespace = [string]$rootNamespaceNodes[0].InnerText
        }
        else {
            $rootNamespace = [string]$projectFile.BaseName
        }

        $items = Get-LibraryEvaluatedProjectItems `
            -ProjectPath $projectFile.FullName `
            -TargetFramework $targetFramework

        [void]$result.Add(
            [pscustomobject]@{
                Path = [System.IO.Path]::GetFullPath(
                    $projectFile.FullName
                )
                Directory = [System.IO.Path]::GetFullPath(
                    $projectFile.DirectoryName
                )
                RootNamespace = $rootNamespace
                Compile = @($items.Compile)
                ProjectReference = @($items.ProjectReference)
            }
        )
    }

    return @($result)
}

function Get-ReleaseLibraries {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $projects = @(
        Get-LibrarySourceProjectInfos -RepoRoot $RepoRoot
    )

    $projectLookup = @{}

    foreach ($project in $projects) {
        $projectLookup[$project.Path.ToLowerInvariant()] = $project
    }

    $libraries = New-Object System.Collections.ArrayList
    $assignments = @{}
    $packageClaims = @{}
    $slugClaims = @{}

    $versionFiles = @(
        Get-ChildItem `
            -LiteralPath (Join-Path $RepoRoot "src") `
            -Recurse `
            -File `
            -Filter "LibraryVersionFile.cs" |
            Where-Object {
                $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
            } |
            Sort-Object FullName
    )

    foreach ($versionFile in $versionFiles) {
        $adjacentProjects = @(
            Get-ChildItem `
                -LiteralPath $versionFile.DirectoryName `
                -File `
                -Filter "*.csproj" |
                Sort-Object FullName
        )

        if ($adjacentProjects.Count -ne 1) {
            throw (
                "Library version file '$($versionFile.FullName)' must have " +
                "exactly one adjacent project."
            )
        }

        $rootProjectPath = [System.IO.Path]::GetFullPath(
            $adjacentProjects[0].FullName
        )

        $rootProjectKey = $rootProjectPath.ToLowerInvariant()

        if (-not $projectLookup.ContainsKey($rootProjectKey)) {
            throw "Package root project was not discovered: $rootProjectPath"
        }

        $rootProject = $projectLookup[$rootProjectKey]

        $descriptor = Read-LibraryVersionDescriptor `
            -Path $versionFile.FullName

        if (
            -not (
                $rootProject.RootNamespace.Equals(
                    $descriptor.PackageId,
                    [System.StringComparison]::OrdinalIgnoreCase
                ) -or
                $rootProject.RootNamespace.StartsWith(
                    $descriptor.PackageId + ".",
                    [System.StringComparison]::OrdinalIgnoreCase
                )
            )
        ) {
            throw (
                "Root project '$rootProjectPath' has RootNamespace " +
                "'$($rootProject.RootNamespace)', which does not match " +
                "package namespace '$($descriptor.PackageId)'."
            )
        }

        $packageKey = $descriptor.PackageId.ToLowerInvariant()
        $slugKey = $descriptor.Slug.ToLowerInvariant()

        if ($packageClaims.ContainsKey($packageKey)) {
            throw (
                "Package namespace '$($descriptor.PackageId)' is declared " +
                "by more than one LibraryVersionFile.cs."
            )
        }

        if ($slugClaims.ContainsKey($slugKey)) {
            throw (
                "Release slug '$($descriptor.Slug)' is produced by both " +
                "'$($slugClaims[$slugKey])' and " +
                "'$($descriptor.PackageId)'."
            )
        }

        $library = [pscustomobject]@{
            PackageId = $descriptor.PackageId
            Slug = $descriptor.Slug
            Version = $descriptor.Version
            Changelog = @($descriptor.Changelog)
            VersionFilePath = $descriptor.VersionFilePath
            RootProjectPath = $rootProjectPath
            Projects = New-Object System.Collections.ArrayList
            SourceRoots = New-Object System.Collections.ArrayList
        }

        [void]$library.Projects.Add($rootProject)
        [void]$library.SourceRoots.Add($rootProject.Directory)

        $packageClaims[$packageKey] = $library
        $slugClaims[$slugKey] = $descriptor.PackageId
        $assignments[$rootProjectKey] = $library

        [void]$libraries.Add($library)
    }

    if ($libraries.Count -eq 0) {
        throw "No src/**/LibraryVersionFile.cs package roots were found."
    }

    $unassigned = @(
        $projects |
            Where-Object {
                -not $assignments.ContainsKey(
                    $_.Path.ToLowerInvariant()
                )
            }
    )

    do {
        $assignedThisPass = 0
        $remaining = New-Object System.Collections.ArrayList

        foreach ($project in $unassigned) {
            $candidateMap = @{}

            foreach ($reference in @($project.ProjectReference)) {
                $referenceOutputAssembly = (
                    [string](Get-LibraryObjectPropertyValue -Object $reference -Name "ReferenceOutputAssembly")
                )

                if (
                    -not [string]::IsNullOrWhiteSpace(
                        $referenceOutputAssembly
                    ) -and
                    $referenceOutputAssembly.Equals(
                        "false",
                        [System.StringComparison]::OrdinalIgnoreCase
                    )
                ) {
                    continue
                }

                $referencePath = [System.IO.Path]::GetFullPath(
                    [string](Get-LibraryObjectPropertyValue -Object $reference -Name "FullPath")
                )

                $referenceKey = $referencePath.ToLowerInvariant()

                if ($assignments.ContainsKey($referenceKey)) {
                    $candidate = $assignments[$referenceKey]

                    $candidateMap[
                        $candidate.PackageId.ToLowerInvariant()
                    ] = $candidate
                }
            }

            $namespaceMatches = @(
                $candidateMap.GetEnumerator() |
                    ForEach-Object { $_.Value } |
                    Where-Object {
                        $project.RootNamespace.Equals(
                            $_.PackageId,
                            [System.StringComparison]::OrdinalIgnoreCase
                        ) -or
                        $project.RootNamespace.StartsWith(
                            $_.PackageId + ".",
                            [System.StringComparison]::OrdinalIgnoreCase
                        )
                    }
            )

            if ($namespaceMatches.Count -eq 0) {
                [void]$remaining.Add($project)
                continue
            }

            if ($namespaceMatches.Count -gt 1) {
                throw (
                    "Project '$($project.Path)' matches more than one " +
                    "package namespace."
                )
            }

            $owner = $namespaceMatches[0]

            [void]$owner.Projects.Add($project)

            if (
                -not (
                    @($owner.SourceRoots) -contains $project.Directory
                )
            ) {
                [void]$owner.SourceRoots.Add($project.Directory)
            }

            $assignments[
                $project.Path.ToLowerInvariant()
            ] = $owner

            $assignedThisPass++
        }

        $unassigned = @($remaining)
    }
    while ($assignedThisPass -gt 0)

    $repoFull = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar
    )

    foreach ($library in $libraries) {
        $library.Projects = @(
            $library.Projects |
                Sort-Object Path
        )

        $library.SourceRoots = @(
            $library.SourceRoots |
                Sort-Object
        )

        $relativeDirectories = New-Object System.Collections.ArrayList

        foreach ($sourceRoot in $library.SourceRoots) {
            if (
                -not (
                    Test-LibraryPathInsideRoot `
                        -Path $sourceRoot `
                        -Root $repoFull
                )
            ) {
                throw (
                    "Package source root '$sourceRoot' is outside " +
                    "repository '$RepoFull'."
                )
            }

            $relative = $sourceRoot.Substring(
                $repoFull.Length
            ).TrimStart(
                [System.IO.Path]::DirectorySeparatorChar,
                [System.IO.Path]::AltDirectorySeparatorChar
            ).Replace("\", "/")

            [void]$relativeDirectories.Add($relative)
        }

        Add-Member `
            -InputObject $library `
            -NotePropertyName SourceDirectories `
            -NotePropertyValue @($relativeDirectories)
    }

    return @(
        $libraries |
            Sort-Object PackageId
    )
}

function Get-LibraryPortableTestProjects {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Library,

        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $testRoot = Join-Path $RepoRoot "tests"

    if (-not (Test-Path -LiteralPath $testRoot -PathType Container)) {
        return @()
    }

    $rootProjectPath = [System.IO.Path]::GetFullPath(
        [string]$Library.RootProjectPath
    )

    $result = New-Object System.Collections.ArrayList

    foreach (
        $projectFile in @(
            Get-ChildItem `
                -LiteralPath $testRoot `
                -Recurse `
                -File `
                -Filter "*.csproj" |
                Where-Object {
                    $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
                } |
                Sort-Object FullName
        )
    ) {
        try {
            [xml]$project = Get-Content `
                -LiteralPath $projectFile.FullName `
                -Raw
        }
        catch {
            throw (
                "Could not read test project '$($projectFile.FullName)': " +
                "$($_.Exception.Message)"
            )
        }

        $targetFramework = Get-LibraryProjectTargetFramework `
            -Project $project `
            -ProjectPath $projectFile.FullName

        $items = Get-LibraryEvaluatedProjectItems `
            -ProjectPath $projectFile.FullName `
            -TargetFramework $targetFramework

        $referencesRoot = $false

        foreach ($reference in @($items.ProjectReference)) {
            $referenceOutputAssembly = [string](
                Get-LibraryObjectPropertyValue `
                    -Object $reference `
                    -Name "ReferenceOutputAssembly"
            )

            if (
                -not [string]::IsNullOrWhiteSpace(
                    $referenceOutputAssembly
                ) -and
                $referenceOutputAssembly.Equals(
                    "false",
                    [System.StringComparison]::OrdinalIgnoreCase
                )
            ) {
                continue
            }

            $referencePathValue = [string](
                Get-LibraryObjectPropertyValue `
                    -Object $reference `
                    -Name "FullPath"
            )

            if ([string]::IsNullOrWhiteSpace($referencePathValue)) {
                throw (
                    "Test project '$($projectFile.FullName)' contains a " +
                    "ProjectReference without FullPath metadata."
                )
            }

            $referencePath = [System.IO.Path]::GetFullPath(
                $referencePathValue
            )

            if (
                $referencePath.Equals(
                    $rootProjectPath,
                    [System.StringComparison]::OrdinalIgnoreCase
                )
            ) {
                $referencesRoot = $true
                break
            }
        }

        if ($referencesRoot) {
            [void]$result.Add($projectFile)
        }
    }

    return @(
        $result |
            Sort-Object FullName
    )
}
function Get-SELibsLockContext {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $manifestPath = Join-Path $RepoRoot "selibs.json"
    $lockPath = Join-Path $RepoRoot "selibs.lock.json"

    $manifestExists = Test-Path `
        -LiteralPath $manifestPath `
        -PathType Leaf

    $lockExists = Test-Path `
        -LiteralPath $lockPath `
        -PathType Leaf

    if (-not $manifestExists -and -not $lockExists) {
        return $null
    }

    if (-not $manifestExists -or -not $lockExists) {
        throw (
            "External SELibs dependency detection requires both " +
            "selibs.json and selibs.lock.json."
        )
    }

    try {
        $manifest = Get-Content `
            -LiteralPath $manifestPath `
            -Raw |
            ConvertFrom-Json

        $lock = Get-Content `
            -LiteralPath $lockPath `
            -Raw |
            ConvertFrom-Json
    }
    catch {
        throw "Could not read SELibs metadata: $($_.Exception.Message)"
    }

    $manifestSchemaVersion = Get-LibraryObjectPropertyValue `
        -Object $manifest `
        -Name "schemaVersion"

    $librariesPathValue = Get-LibraryObjectPropertyValue `
        -Object $manifest `
        -Name "librariesPath"

    if (
        $manifestSchemaVersion -ne 1 -or
        [string]::IsNullOrWhiteSpace(
            [string]$librariesPathValue
        )
    ) {
        throw "selibs.json does not use the supported schema."
    }

    $lockSchemaVersion = Get-LibraryObjectPropertyValue `
        -Object $lock `
        -Name "schemaVersion"

    $lockPackages = Get-LibraryObjectPropertyValue `
        -Object $lock `
        -Name "packages"

    if ($lockSchemaVersion -ne 1 -or $null -eq $lockPackages) {
        throw "selibs.lock.json does not use the supported schema."
    }

    $librariesRoot = [System.IO.Path]::GetFullPath(
        (
            Join-Path `
                $RepoRoot `
                ([string]$librariesPathValue)
        )
    )

    $folderOwners = @{}

    foreach ($packageProperty in $lockPackages.PSObject.Properties) {
        $packageId = [string]$packageProperty.Name
        $entry = $packageProperty.Value

        $version = [string](
            Get-LibraryObjectPropertyValue `
                -Object $entry `
                -Name "version"
        )

        if ($version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+$') {
            throw (
                "Locked package '$packageId' does not use an exact " +
                "numeric version."
            )
        }

        $foldersValue = Get-LibraryObjectPropertyValue `
            -Object $entry `
            -Name "folders"

        if ($null -eq $foldersValue) {
            throw "Locked package '$packageId' does not define folders."
        }

        foreach ($folderValue in @($foldersValue)) {
            $folder = [string]$folderValue
            $folderKey = $folder.ToLowerInvariant()

            if ($folderOwners.ContainsKey($folderKey)) {
                throw (
                    "SELibs folder '$folder' is owned by more than one " +
                    "locked package."
                )
            }

            $folderOwners[$folderKey] = [pscustomobject]@{
                PackageId = $packageId
                Version = $version
            }
        }
    }

    return [pscustomobject]@{
        LibrariesRoot = $librariesRoot
        FolderOwners = $folderOwners
    }
}
function Get-SELibsLockedOwnerForPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [AllowNull()]
        [object]$LockContext
    )

    if ($null -eq $LockContext) {
        return $null
    }

    if (
        -not (
            Test-LibraryPathInsideRoot `
                -Path $Path `
                -Root $LockContext.LibrariesRoot
        )
    ) {
        return $null
    }

    $librariesRoot = $LockContext.LibrariesRoot.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar
    )

    $relativePath = [System.IO.Path]::GetFullPath($Path).Substring(
        $librariesRoot.Length
    ).TrimStart(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar
    )

    $separators = [char[]]@(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar
    )

    $segments = @(
        $relativePath.Split(
            $separators,
            [System.StringSplitOptions]::RemoveEmptyEntries
        )
    )

    if ($segments.Count -eq 0) {
        throw "Could not identify SELibs folder for '$Path'."
    }

    $folder = [string]$segments[0]
    $folderKey = $folder.ToLowerInvariant()

    if (-not $LockContext.FolderOwners.ContainsKey($folderKey)) {
        throw (
            "Compiled SELibs folder '$folder' is not owned by any " +
            "package in selibs.lock.json."
        )
    }

    return $LockContext.FolderOwners[$folderKey]
}

function Add-LibraryDependency {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$Dependencies,

        [Parameter(Mandatory = $true)]
        [hashtable]$DependencyKeys,

        [Parameter(Mandatory = $true)]
        [string]$PackageId,

        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    $key = $PackageId.ToLowerInvariant()

    if ($DependencyKeys.ContainsKey($key)) {
        $existingId = [string]$DependencyKeys[$key]
        $existingVersion = [string]$Dependencies[$existingId]

        if ($existingVersion -ne $Version) {
            throw (
                "Dependency conflict for '$PackageId': " +
                "'$existingVersion' and '$Version'."
            )
        }

        return
    }

    $DependencyKeys[$key] = $PackageId
    $Dependencies[$PackageId] = $Version
}

function Resolve-LibraryDependencies {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Library,

        [Parameter(Mandatory = $true)]
        [object[]]$Libraries,

        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $projectOwners = @{}
    $sourceOwners = New-Object System.Collections.ArrayList

    foreach ($candidate in $Libraries) {
        foreach ($project in @($candidate.Projects)) {
            $projectOwners[
                $project.Path.ToLowerInvariant()
            ] = $candidate
        }

        foreach ($sourceRoot in @($candidate.SourceRoots)) {
            [void]$sourceOwners.Add(
                [pscustomobject]@{
                    Root = $sourceRoot
                    Library = $candidate
                }
            )
        }
    }

    $lockContext = Get-SELibsLockContext -RepoRoot $RepoRoot
    $dependencies = [ordered]@{}
    $dependencyKeys = @{}

    foreach ($project in @($Library.Projects)) {
        foreach ($reference in @($project.ProjectReference)) {
            $referenceOutputAssembly = (
                [string](Get-LibraryObjectPropertyValue -Object $reference -Name "ReferenceOutputAssembly")
            )

            if (
                -not [string]::IsNullOrWhiteSpace(
                    $referenceOutputAssembly
                ) -and
                $referenceOutputAssembly.Equals(
                    "false",
                    [System.StringComparison]::OrdinalIgnoreCase
                )
            ) {
                continue
            }

            $referencePath = [System.IO.Path]::GetFullPath(
                [string](Get-LibraryObjectPropertyValue -Object $reference -Name "FullPath")
            )

            $referenceKey = $referencePath.ToLowerInvariant()
            $dependency = $null

            if ($projectOwners.ContainsKey($referenceKey)) {
                $dependency = $projectOwners[$referenceKey]
            }
            else {
                $dependency = Get-SELibsLockedOwnerForPath `
                    -Path $referencePath `
                    -LockContext $lockContext

                if ($null -eq $dependency) {
                    $versionFilePath = Join-Path `
                        (Split-Path -Parent $referencePath) `
                        "LibraryVersionFile.cs"

                    if (
                        -not (
                            Test-Path `
                                -LiteralPath $versionFilePath `
                                -PathType Leaf
                        )
                    ) {
                        throw (
                            "Project reference '$referencePath' is not " +
                            "owned by a discovered package, selibs.lock.json, " +
                            "or an adjacent LibraryVersionFile.cs."
                        )
                    }

                    $dependency = Read-LibraryVersionDescriptor `
                        -Path $versionFilePath
                }
            }

            if (
                $dependency.PackageId.Equals(
                    $Library.PackageId,
                    [System.StringComparison]::OrdinalIgnoreCase
                )
            ) {
                continue
            }

            Add-LibraryDependency `
                -Dependencies $dependencies `
                -DependencyKeys $dependencyKeys `
                -PackageId ([string]$dependency.PackageId) `
                -Version ([string]$dependency.Version)
        }

        foreach ($compileItem in @($project.Compile)) {
            $sourcePath = [System.IO.Path]::GetFullPath(
                [string](Get-LibraryObjectPropertyValue -Object $compileItem -Name "FullPath")
            )

            if ($sourcePath -match '[\\/](bin|obj)[\\/]') {
                continue
            }

            $owned = $false

            foreach ($sourceRoot in @($Library.SourceRoots)) {
                if (
                    Test-LibraryPathInsideRoot `
                        -Path $sourcePath `
                        -Root $sourceRoot
                ) {
                    $owned = $true
                    break
                }
            }

            if ($owned) {
                continue
            }

            $dependency = $null

            foreach ($sourceOwner in @($sourceOwners)) {
                if (
                    Test-LibraryPathInsideRoot `
                        -Path $sourcePath `
                        -Root $sourceOwner.Root
                ) {
                    $dependency = $sourceOwner.Library
                    break
                }
            }

            if ($null -eq $dependency) {
                $dependency = Get-SELibsLockedOwnerForPath `
                    -Path $sourcePath `
                    -LockContext $lockContext
            }

            if ($null -eq $dependency) {
                throw (
                    "Compiled source '$sourcePath' is outside package " +
                    "'$($Library.PackageId)' and cannot be mapped to a " +
                    "discovered package or selibs.lock.json."
                )
            }

            if (
                $dependency.PackageId.Equals(
                    $Library.PackageId,
                    [System.StringComparison]::OrdinalIgnoreCase
                )
            ) {
                continue
            }

            Add-LibraryDependency `
                -Dependencies $dependencies `
                -DependencyKeys $dependencyKeys `
                -PackageId ([string]$dependency.PackageId) `
                -Version ([string]$dependency.Version)
        }
    }

    $ordered = [ordered]@{}

    foreach ($packageId in @($dependencies.Keys | Sort-Object)) {
        $ordered[$packageId] = [string]$dependencies[$packageId]
    }

    return $ordered
}

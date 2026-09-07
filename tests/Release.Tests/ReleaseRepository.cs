using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpaceEngineersModLibraries.ReleaseTests;

internal sealed class ReleaseRepository
{
    private static readonly Lazy<ReleaseRepository> _instance = new(Create);

    private ReleaseRepository(string rootPath, IReadOnlyDictionary<string, LibraryReleaseDescriptor> libraries)
    {
        RootPath = rootPath;
        Libraries = libraries;
    }

    public string RootPath { get; }

    public IReadOnlyDictionary<string, LibraryReleaseDescriptor> Libraries { get; }

    public static ReleaseRepository Load() => _instance.Value;

    public LibraryReleaseDescriptor GetLibrary(string packageId)
    {
        if (Libraries.TryGetValue(packageId, out LibraryReleaseDescriptor? library))
            return library;
        
        throw new InvalidOperationException($"Package '{packageId}' was not discovered.");
    }

    public string[] ListTags(string pattern)
    {
        string output = RunGit("tag", "--list", pattern);

        return output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .ToArray();
    }

    public string ResolveTagCommit(string tag) 
        => RunGit("rev-parse", "--verify", $"refs/tags/{tag}^{{}}").Trim();

    public string ReadFileAtCommit(string commit, string relativePath) 
        => RunGit("show", $"{commit}:{relativePath}");

    private static ReleaseRepository Create()
    {
        string rootPath = FindRepositoryRoot();

        string sourceRoot = Path.Combine(rootPath, "src");

        var libraries =
            Directory
                .EnumerateFiles(sourceRoot, "LibraryVersionFile.cs", SearchOption.AllDirectories)
                .Select(path => LibraryReleaseDescriptor.Parse(
                            File.ReadAllText(path), 
                            Path.GetRelativePath(rootPath, path).Replace('\\', '/')
                        )
                )
                .ToDictionary(library => library.PackageId, StringComparer.Ordinal);

        if (libraries.Count != 0)
            return new(rootPath, libraries);
        
        throw new InvalidOperationException("No LibraryVersionFile.cs package roots were discovered.");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "SpaceEngineersModLibraries.slnx")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate the library repository root.");
    }

    private string RunGit(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = RootPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start git.");

        string standardOutput = process.StandardOutput.ReadToEnd();

        string standardError = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"git {string.Join(" ", arguments)} failed with exit code {process.ExitCode}: {standardError.Trim()}");

        return standardOutput.TrimEnd();
    }
}

internal sealed partial class LibraryReleaseDescriptor
{
    [GeneratedRegex(@"(?m)^\s*namespace\s+(?<value>[A-Za-z_][A-Za-z0-9_.]*)\s*(?:\{|$)", RegexOptions.Compiled)]
    private static partial Regex NamespacePattern();
    [GeneratedRegex(@"(?s)new\s+ChangelogEntry\s*\(\s*""(?<version>(?:\\.|[^""\\])*)""\s*,\s*new\s*\[\]\s*\{(?<changes>.*?)\}\s*\)", RegexOptions.Compiled)]
    private static partial Regex ChangelogEntryPattern();
    [GeneratedRegex(@"""(?<value>(?:\\.|[^""\\])*)""", RegexOptions.Compiled)]
    private static partial Regex StringPattern();
    
    private LibraryReleaseDescriptor(string packageId, string version, string relativeVersionFilePath,
                                     IReadOnlyList<ChangelogEntryDescriptor> changelog)
    {
        PackageId = packageId;
        Version = version;
        RelativeVersionFilePath = relativeVersionFilePath;
        Changelog = changelog;
    }

    public string PackageId { get; }

    public string Version { get; }

    public string RelativeVersionFilePath { get; }

    public IReadOnlyList<ChangelogEntryDescriptor> Changelog { get; }

    public static LibraryReleaseDescriptor Parse(string source, string relativeVersionFilePath)
    {
        Match namespaceMatch = NamespacePattern().Match(source);

        if (!namespaceMatch.Success)
            throw new InvalidOperationException($"'{relativeVersionFilePath}' does not declare a supported namespace.");

        string packageId = namespaceMatch.Groups["value"].Value;

        int major = ReadVersionPart(source, relativeVersionFilePath, "Major");
        int minor = ReadVersionPart(source, relativeVersionFilePath, "Minor");
        int patch = ReadVersionPart(source, relativeVersionFilePath, "Patch");

        var version = $"{major}.{minor}.{patch}";

        var changelog = ChangelogEntryPattern()
            .Matches(source)
            .Select(match => new ChangelogEntryDescriptor(
                        Regex.Unescape(match.Groups["version"].Value), 
                        StringPattern()
                            .Matches(match.Groups["changes"].Value)
                            .Select(change => Regex.Unescape(change.Groups["value"].Value))
                            .ToArray()
                    )
                )
            .ToArray();

        return new(packageId, version, relativeVersionFilePath, changelog);
    }

    public string[] FlattenChangelog() 
        => Changelog.SelectMany(entry => new[] { $"version:{entry.Version}" }
                                    .Concat(entry.Changes.Select(change => $"change:{change}")))
                    .ToArray();

    private static int ReadVersionPart(string source, string relativeVersionFilePath, string name)
    {
        Match match = Regex.Match(source, $@"public\s+const\s+int\s+{Regex.Escape(name)}\s*=\s*(?<value>[0-9]+)\s*;");

        if (!match.Success)
            throw new InvalidOperationException($"'{relativeVersionFilePath}' does not declare numeric {name}.");

        return int.Parse(match.Groups["value"].Value);
    }
}

internal sealed class ChangelogEntryDescriptor(string version, IReadOnlyList<string> changes)
{
    public string Version { get; } = version;

    public IReadOnlyList<string> Changes { get; } = changes;
}

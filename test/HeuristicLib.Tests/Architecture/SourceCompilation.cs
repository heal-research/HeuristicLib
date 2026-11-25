using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HEAL.HeuristicLib.Tests.Architecture;

internal static class SourceCompilation
{
    internal static (CSharpCompilation Compilation, SyntaxTree[] Trees) CreateMainCompilation(string repositoryRoot)
        => CreateCompilation(repositoryRoot, "HeuristicLib", includeMain: false);

    internal static (CSharpCompilation Compilation, SyntaxTree[] Trees) CreateCompilation(
        string repositoryRoot, string projectFolder, bool includeMain)
    {
        var sourceRoot = Path.Combine(repositoryRoot, "src", projectFolder);
        var trees = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(sourceRoot, path).Split(Path.DirectorySeparatorChar)
                .Any(segment => segment is "obj" or "bin"))
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), SourceParseOptions, path))
            .ToArray();

        var assetsPath = Path.Combine(repositoryRoot, "src", projectFolder, "obj", "project.assets.json");
        SyntaxTree[] generatedUsings = projectFolder == "HeuristicLib" ? [GlobalUsings, MainOnlyGlobalUsings] : [GlobalUsings];
        var compilation = CSharpCompilation.Create(projectFolder + "LayerCheck", [.. generatedUsings, .. trees],
            MetadataReferences(includeMain, assetsPath), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return (compilation, trees);
    }

    internal static readonly CSharpParseOptions SourceParseOptions = new(LanguageVersion.Preview);

    private static readonly SyntaxTree GlobalUsings = CSharpSyntaxTree.ParseText(
        """
        global using System;
        global using System.Collections.Generic;
        global using System.Collections.Immutable;
        global using System.IO;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Threading;
        global using System.Threading.Tasks;
        global using HEAL.HeuristicLib.Collections;
        """, SourceParseOptions);

    private static readonly SyntaxTree MainOnlyGlobalUsings = CSharpSyntaxTree.ParseText(
        "global using static Parlot.Fluent.Parsers;", SourceParseOptions);

    private static MetadataReference[] MetadataReferences(bool includeMain, string? assetsPath = null)
    {
        var platform = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(path => !Path.GetFileName(path).StartsWith("HEAL.HeuristicLib", StringComparison.Ordinal) &&
                           Path.GetFileName(path) != "System.Linq.AsyncEnumerable.dll");
        var other = Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll")
            .Where(path => !Path.GetFileName(path).StartsWith("HEAL.HeuristicLib", StringComparison.Ordinal));
        var packages = assetsPath is null ? [] : PackageCompilePaths(assetsPath);
        var paths = platform.Concat(other).Concat(packages)
            .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        if (includeMain)
            paths.Add(typeof(IAlgorithm<>).Assembly.Location);

        return [.. paths.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))];
    }

    private static string[] PackageCompilePaths(string assetsPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(assetsPath));
        var root = document.RootElement;
        var packageFolders = root.GetProperty("packageFolders").EnumerateObject().Select(folder => folder.Name).ToArray();
        var libraries = root.GetProperty("libraries");
        var target = root.GetProperty("targets").EnumerateObject().First().Value;
        var paths = new List<string>();

        foreach (var dependency in target.EnumerateObject())
        {
            if (!libraries.TryGetProperty(dependency.Name, out var library) ||
                library.GetProperty("type").GetString() != "package" ||
                !dependency.Value.TryGetProperty("compile", out var compile))
                continue;

            var packagePath = library.GetProperty("path").GetString()!;
            foreach (var asset in compile.EnumerateObject())
            {
                if (!asset.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                var path = packageFolders.Select(folder => Path.Combine(folder, packagePath, asset.Name))
                    .FirstOrDefault(File.Exists);
                if (path is not null)
                    paths.Add(path);
            }
        }

        return [.. paths];
    }

    internal static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "HEAL.HeuristicLib.slnx")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}

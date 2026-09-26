using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HEAL.HeuristicLib.Tests.Architecture;

public sealed class LayerDependencyTests
{
    private const string MainRoot = "src/HeuristicLib/";
    private const string ExperimentalRoot = "src/HeuristicLib.Experimental/";

    // Each entry is one source file and forbidden dependency. Remove entries as the corresponding steps land.
    private static readonly string[] ExistingViolations =
    [
        "src/HeuristicLib.Experimental/Problems/Dynamic/DynamicProblem.cs | Problems -> Analysis",
    ];

    [Fact]
    public void SourceDependencies_MatchTheTemporaryBaseline()
    {
        var actual = FindViolations();
        actual.ShouldBe(ExistingViolations, ignoreOrder: true);
    }

    [Fact]
    public void MainAssembly_DoesNotReferenceExperimental() =>
        typeof(IAlgorithm<>).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ShouldNotContain("HEAL.HeuristicLib.Experimental");

    [Fact]
    public void SemanticCheck_CatchesAQualifiedForbiddenReference()
    {
        var repositoryRoot = FindRepositoryRoot();
        var (compilation, _) = CreateMainCompilation(repositoryRoot);
        var probe = CSharpSyntaxTree.ParseText(
            "namespace HEAL.HeuristicLib.Operators; internal class LayerProbe { private HEAL.HeuristicLib.Analysis.IAnalyzer? analyzer; }",
            ParseOptions,
            Path.Combine(repositoryRoot, MainRoot, "Operators", "LayerProbe.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        compilation = compilation.AddSyntaxTrees(probe);

        ScanTree(compilation, probe, repositoryRoot).ShouldContain("src/HeuristicLib/Operators/LayerProbe.cs | Operators -> Analysis");
    }

    [Fact]
    public void SemanticCheck_CatchesGlobalUsingAndInternalAccess()
    {
        var repositoryRoot = FindRepositoryRoot();
        var (compilation, _) = CreateMainCompilation(repositoryRoot);
        var probe = CSharpSyntaxTree.ParseText(
            "global using HEAL.HeuristicLib.Analysis; namespace HEAL.HeuristicLib.Operators; internal class LayerProbe { int Depth => HEAL.HeuristicLib.Execution.ResolutionScope.Create().Depth; }",
            ParseOptions,
            Path.Combine(repositoryRoot, MainRoot, "Operators", "LayerProbe.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        compilation = compilation.AddSyntaxTrees(probe);

        var violations = ScanTree(compilation, probe, repositoryRoot).ToArray();
        violations.ShouldContain("src/HeuristicLib/Operators/LayerProbe.cs | Operators -> Analysis");
        violations.ShouldContain("src/HeuristicLib/Operators/LayerProbe.cs | OutsideExecution -> ResolutionInternal");
    }

    [Fact]
    public void SemanticCheck_DistinguishesConcreteAlgorithmsAndEncodings()
    {
        var repositoryRoot = FindRepositoryRoot();
        var (compilation, _) = CreateMainCompilation(repositoryRoot);
        var hostProbe = CSharpSyntaxTree.ParseText(
            "namespace HEAL.HeuristicLib.Execution; internal class HostProbe { private HEAL.HeuristicLib.Algorithms.GeneticAlgorithm<int>? algorithm; int Depth => ResolutionScope.Create().Depth; }",
            ParseOptions,
            Path.Combine(repositoryRoot, MainRoot, "Execution", "Runs", "HostProbe.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        var objectiveProbe = CSharpSyntaxTree.ParseText(
            "namespace HEAL.HeuristicLib.Objectives; internal class ObjectiveProbe { private HEAL.HeuristicLib.Encodings.Permutations.Permutation? order; }",
            ParseOptions,
            Path.Combine(repositoryRoot, MainRoot, "Objectives", "ObjectiveProbe.cs"),
            cancellationToken: TestContext.Current.CancellationToken);
        compilation = compilation.AddSyntaxTrees(hostProbe, objectiveProbe);

        ScanTree(compilation, hostProbe, repositoryRoot)
            .ShouldContain("src/HeuristicLib/Execution/Runs/HostProbe.cs | Hosting -> ConcreteAlgorithm");
        ScanTree(compilation, hostProbe, repositoryRoot)
            .ShouldContain("src/HeuristicLib/Execution/Runs/HostProbe.cs | OutsideExecution -> ResolutionInternal");
        ScanTree(compilation, objectiveProbe, repositoryRoot)
            .ShouldContain("src/HeuristicLib/Objectives/ObjectiveProbe.cs | Objectives -> ConcreteEncoding");
    }

    private static string[] FindViolations()
    {
        var repositoryRoot = FindRepositoryRoot();
        var (main, mainTrees) = CreateMainCompilation(repositoryRoot);
        main.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString()).ShouldBeEmpty();
        var violations = mainTrees.SelectMany(tree => ScanTree(main, tree, repositoryRoot)).ToList();

        var (experimental, experimentalTrees) = CreateCompilation(repositoryRoot, "HeuristicLib.Experimental", includeMain: true);
        experimental.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString()).ShouldBeEmpty();
        violations.AddRange(experimentalTrees.SelectMany(tree => ScanTree(experimental, tree, repositoryRoot)));

        return [.. violations.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }

    private static (CSharpCompilation Compilation, SyntaxTree[] Trees) CreateMainCompilation(string repositoryRoot)
        => CreateCompilation(repositoryRoot, "HeuristicLib", includeMain: false);

    private static (CSharpCompilation Compilation, SyntaxTree[] Trees) CreateCompilation(
        string repositoryRoot, string projectFolder, bool includeMain)
    {
        var sourceRoot = Path.Combine(repositoryRoot, "src", projectFolder);
        var trees = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(sourceRoot, path).Split(Path.DirectorySeparatorChar)
                .Any(segment => segment is "obj" or "bin"))
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), ParseOptions, path))
            .ToArray();

        var assetsPath = Path.Combine(repositoryRoot, "src", projectFolder, "obj", "project.assets.json");
        SyntaxTree[] generatedUsings = projectFolder == "HeuristicLib" ? [GlobalUsings, MainOnlyGlobalUsings] : [GlobalUsings];
        var compilation = CSharpCompilation.Create(projectFolder + "LayerCheck", [.. generatedUsings, .. trees],
            MetadataReferences(includeMain, assetsPath), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return (compilation, trees);
    }

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);

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
        """, ParseOptions);

    private static readonly SyntaxTree MainOnlyGlobalUsings = CSharpSyntaxTree.ParseText(
        "global using static Parlot.Fluent.Parsers;", ParseOptions);

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

    private static IEnumerable<string> ScanTree(CSharpCompilation compilation, SyntaxTree tree, string repositoryRoot)
    {
        var path = Path.GetRelativePath(repositoryRoot, tree.FilePath).Replace('\\', '/');
        var root = tree.GetRoot();
        var model = compilation.GetSemanticModel(tree);
        var sourceKind = SourceKind(path, root);
        var found = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in root.DescendantNodes())
        {
            if (node is not (SimpleNameSyntax or ObjectCreationExpressionSyntax or UsingDirectiveSyntax))
                continue;

            var symbol = model.GetSymbolInfo(node).Symbol;
            if (symbol is null && node is UsingDirectiveSyntax usingDirective && usingDirective.Name is not null)
                symbol = model.GetSymbolInfo(usingDirective.Name).Symbol;
            if (symbol is null)
                continue;

            var targetType = symbol as INamedTypeSymbol ?? symbol.ContainingType;
            var targetNamespace = (symbol as INamespaceSymbol)?.ToDisplayString() ??
                                  targetType?.ContainingNamespace.ToDisplayString() ?? string.Empty;

            if (sourceKind is "Operators" or "Algorithms" or "Problems" or "Hosting" &&
                targetNamespace.StartsWith("HEAL.HeuristicLib.Analysis", StringComparison.Ordinal))
                found.Add($"{path} | {sourceKind} -> Analysis");

            if (sourceKind == "Objectives" &&
                targetNamespace.StartsWith("HEAL.HeuristicLib.Encodings", StringComparison.Ordinal))
                found.Add($"{path} | Objectives -> ConcreteEncoding");

            if (sourceKind == "Hosting" && targetType is { TypeKind: TypeKind.Class, IsAbstract: false } &&
                targetNamespace.StartsWith("HEAL.HeuristicLib.Algorithms", StringComparison.Ordinal) &&
                targetType.AllInterfaces.Any(@interface => @interface.Name == "IAlgorithm"))
                found.Add($"{path} | Hosting -> ConcreteAlgorithm");

            if (path.StartsWith(MainRoot, StringComparison.Ordinal) && targetType is not null &&
                targetType.Locations.Any(location => location.IsInSource &&
                    location.SourceTree?.FilePath.Replace('\\', '/').Contains(ExperimentalRoot, StringComparison.Ordinal) == true))
                found.Add($"{path} | Main -> Experimental");

            if (IsResolutionInternal(symbol, targetType) &&
                (!path.StartsWith(MainRoot + "Execution/", StringComparison.Ordinal) ||
                 path.StartsWith(MainRoot + "Execution/Runs/", StringComparison.Ordinal) ||
                 path.StartsWith(MainRoot + "Execution/Concurrency/", StringComparison.Ordinal)))
                found.Add($"{path} | OutsideExecution -> ResolutionInternal");

            if (IsRunInternal(symbol, targetType) &&
                !path.StartsWith(MainRoot + "Execution/Runs/", StringComparison.Ordinal) &&
                !path.StartsWith(MainRoot + "Experiments/", StringComparison.Ordinal))
                found.Add($"{path} | OutsideHosting -> RunInternal");
        }

        return found;
    }

    private static bool IsResolutionInternal(ISymbol symbol, INamedTypeSymbol? type) =>
        type?.Name is "ResolutionScope" or "ResolutionScopeBuilder" or "Decoration" &&
        (symbol.DeclaredAccessibility == Accessibility.Internal || type.DeclaredAccessibility == Accessibility.Internal);

    private static bool IsRunInternal(ISymbol symbol, INamedTypeSymbol? type) =>
        type?.Name.StartsWith("AlgorithmRun", StringComparison.Ordinal) == true &&
        symbol.Name is "StreamForTerminalCancellation" or "CancelAsync" &&
        symbol.DeclaredAccessibility == Accessibility.Internal;

    private static string SourceKind(string path, SyntaxNode root)
    {
        if (path.StartsWith(MainRoot + "Objectives/", StringComparison.Ordinal) ||
            path.StartsWith(ExperimentalRoot + "Objectives/", StringComparison.Ordinal))
            return "Objectives";
        if (path.StartsWith(MainRoot + "Operators/", StringComparison.Ordinal) ||
            path.StartsWith(ExperimentalRoot + "Operators/", StringComparison.Ordinal))
            return "Operators";
        if (path.StartsWith(MainRoot + "Algorithms/", StringComparison.Ordinal) ||
            path.StartsWith(ExperimentalRoot + "Algorithms/", StringComparison.Ordinal))
            return "Algorithms";
        if (path.StartsWith(MainRoot + "Problems/", StringComparison.Ordinal) ||
            path.StartsWith(ExperimentalRoot + "Problems/", StringComparison.Ordinal))
            return "Problems";
        if (path.StartsWith(MainRoot + "Execution/Runs/", StringComparison.Ordinal))
            return "Hosting";
        if (path.StartsWith(MainRoot + "Experiments/", StringComparison.Ordinal) &&
            !root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>()
                .Any(declaration => declaration.Name.ToString() == "HEAL.HeuristicLib.Analysis"))
            return "Hosting";
        return "Other";
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "HEAL.HeuristicLib.slnx")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}

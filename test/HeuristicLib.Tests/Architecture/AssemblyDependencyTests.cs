using System.Text.RegularExpressions;

namespace HEAL.HeuristicLib.Tests.Architecture;

public sealed class AssemblyDependencyTests
{
    [Fact]
    public void MainAndContracts_DoNotReferenceExperimental()
    {
        var assemblies = new[]
        {
            typeof(Algorithm<, , , , >).Assembly,
            typeof(IAlgorithm<>).Assembly
        };

        foreach (var assembly in assemblies)
        {
            assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .ShouldNotContain("HEAL.HeuristicLib.Experimental");
        }
    }

    [Fact]
    public void ConceptFolders_UseTheirIntendedNamespaces()
    {
        var repositoryRoot = FindRepositoryRoot();
        var mismatches = new List<string>();

        CheckNamespace(Path.Combine(repositoryRoot, "src", "HeuristicLib", "Algorithms", "SearchStates"), "HEAL.HeuristicLib.Algorithms", mismatches);
        CheckNamespace(Path.Combine(repositoryRoot, "src", "HeuristicLib", "MachineLearning"), "HEAL.HeuristicLib.MachineLearning", mismatches);
        CheckNamespace(Path.Combine(repositoryRoot, "src", "HeuristicLib", "Numerics", "Statistics"), "HEAL.HeuristicLib.Numerics", mismatches);
        CheckNamespace(Path.Combine(repositoryRoot, "src", "HeuristicLib", "Random", "Distributions"), "HEAL.HeuristicLib.Random", mismatches);
        CheckNamespace(Path.Combine(repositoryRoot, "src", "HeuristicLib", "Random", "KeyCombiners"), "HEAL.HeuristicLib.Random", mismatches);
        CheckNamespace(Path.Combine(repositoryRoot, "src", "HeuristicLib", "Random", "RandomEngines"), "HEAL.HeuristicLib.Random", mismatches);
        CheckNamespace(Path.Combine(repositoryRoot, "src", "HeuristicLib", "Analysis", "Quality"), "HEAL.HeuristicLib.Analysis", mismatches);
        CheckNamespace(Path.Combine(repositoryRoot, "src", "HeuristicLib", "Encodings", "SymbolicExpressions"), "HEAL.HeuristicLib.Encodings.SymbolicExpressions", mismatches);
        CheckNamespace(Path.Combine(repositoryRoot, "src", "HeuristicLib.Contracts", "Algorithms", "SearchStates"), "HEAL.HeuristicLib.Algorithms", mismatches);

        // Operator defaults sit with what declares them rather than with the factories that read them, so a search
        // space or problem states its defaults without importing the algorithms namespace to describe itself.
        CheckNamespace(Path.Combine(repositoryRoot, "src", "HeuristicLib.Contracts", "SearchSpaces"), "HEAL.HeuristicLib.SearchSpaces", mismatches);
        CheckNamespace(Path.Combine(repositoryRoot, "src", "HeuristicLib.Contracts", "Problems"), "HEAL.HeuristicLib.Problems", mismatches);

        typeof(ICrossover<>).Namespace.ShouldBe("HEAL.HeuristicLib.Operators");
        typeof(Parents<>).Namespace.ShouldBe("HEAL.HeuristicLib.Operators");

        var encodingsRoot = Path.Combine(repositoryRoot, "src", "HeuristicLib", "Encodings");
        foreach (var encoding in Directory.EnumerateDirectories(encodingsRoot))
        {
            var concept = Path.GetFileName(encoding);
            CheckNamespacePrefix(encoding, $"HEAL.HeuristicLib.Encodings.{concept}", mismatches);
        }

        mismatches.ShouldBeEmpty();
    }

    [Fact]
    public void ProjectRootsAndEncodingGroupingFolder_DoNotOwnSourceFiles()
    {
        var repositoryRoot = FindRepositoryRoot();
        var folders = new[]
        {
            Path.Combine(repositoryRoot, "src", "HeuristicLib"),
            Path.Combine(repositoryRoot, "src", "HeuristicLib.Contracts"),
            Path.Combine(repositoryRoot, "src", "HeuristicLib", "Encodings"),
        };

        folders.SelectMany(folder => Directory.EnumerateFiles(folder, "*.cs", SearchOption.TopDirectoryOnly))
            .ShouldBeEmpty();
    }

    [Fact]
    public void MainAssembly_OwnsStableBoundaryTypesOnly()
    {
        var main = typeof(GeneticAlgorithm<>).Assembly;

        main.GetType("HEAL.HeuristicLib.Problems.MachineLearning.SymbolicRegressionProblem").ShouldNotBeNull();
        main.GetType("HEAL.HeuristicLib.Algorithms.NSGA2`1").ShouldNotBeNull();
        main.GetType("HEAL.HeuristicLib.Problems.QuadraticAssignment.QuadraticAssignmentProblem").ShouldNotBeNull();
        main.GetType("HEAL.HeuristicLib.Algorithms.AlpsGeneticAlgorithm`1").ShouldBeNull();
        main.GetType("HEAL.HeuristicLib.Analysis.PopulationSimilarityAnalyzer`4").ShouldBeNull();
        main.GetType("HEAL.HeuristicLib.SearchSpaces.ISubencodingComparable`1").ShouldBeNull();
        main.GetType("HEAL.HeuristicLib.Algorithms.ISolutionLayout`1").ShouldBeNull();
    }

    [Fact]
    public void ModernSourceTestsAndUserDocumentation_DoNotUseLegacyDataNamespace()
    {
        var repositoryRoot = FindRepositoryRoot();
        var roots = new[]
        {
            Path.Combine(repositoryRoot, "src", "HeuristicLib"),
            Path.Combine(repositoryRoot, "src", "HeuristicLib.Contracts"),
            Path.Combine(repositoryRoot, "test", "HeuristicLib.Tests"),
            Path.Combine(repositoryRoot, "test", "HeuristicLib.Tests.ApiUsageSpecs"),
            Path.Combine(repositoryRoot, "docs", "guide"),
            Path.Combine(repositoryRoot, "docs", "examples"),
            Path.Combine(repositoryRoot, "examples")
        };

        var files = roots.SelectMany(root => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            .Append(Path.Combine(repositoryRoot, "README.md"))
            .Where(path => Path.GetExtension(path) is ".cs" or ".md");

        var legacyNamespace = string.Concat("Data", "Analysis");
        files.Where(path => File.ReadAllText(path).Contains(legacyNamespace, StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ShouldBeEmpty();
    }

    private static void CheckNamespace(string folder, string expected, ICollection<string> mismatches)
    {
        foreach (var file in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            var actual = ReadNamespace(file);
            if (actual != expected)
                mismatches.Add($"{Path.GetRelativePath(folder, file)}: {actual} != {expected}");
        }
    }

    private static void CheckNamespacePrefix(string folder, string expectedPrefix, ICollection<string> mismatches)
    {
        foreach (var file in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            var actual = ReadNamespace(file);
            if (actual != expectedPrefix && !actual.StartsWith($"{expectedPrefix}.", StringComparison.Ordinal))
                mismatches.Add($"{Path.GetRelativePath(folder, file)}: {actual} does not start with {expectedPrefix}");
        }
    }

    private static string ReadNamespace(string file)
    {
        var match = Regex.Match(File.ReadAllText(file), @"(?m)^namespace\s+([A-Za-z0-9_.]+)");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "HEAL.HeuristicLib.sln")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}

using HEAL.HeuristicLib.Encodings.Composite;
using HEAL.HeuristicLib.Encodings.Empty;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;

namespace HEAL.HeuristicLib.Tests.Architecture;

public sealed class ExperimentalBoundaryTests
{
    [Fact]
    public void ResearchFeatures_AreOwnedByExperimentalAssembly()
    {
        var experimental = typeof(AlpsGeneticAlgorithm<,,>).Assembly;

        typeof(OpenEndedRelevantAllelesPreservingGeneticAlgorithm<,,>).Assembly.ShouldBe(experimental);
        typeof(IslandPopulation<>).Assembly.ShouldBe(experimental);
        experimental.GetType("HEAL.HeuristicLib.Analysis.PopulationSimilarityAnalyzer`4").ShouldNotBeNull();
        experimental.GetType("HEAL.HeuristicLib.Analysis.HyperVolumeAnalysis`3").ShouldNotBeNull();
    }

    [Fact]
    public void StaticQuadraticAssignment_IsOwnedByTheMainAssembly()
    {
        typeof(QuadraticAssignmentProblem).Assembly.ShouldBe(typeof(GeneticAlgorithm<,,>).Assembly);
    }

    [Fact]
    public void ReorganizedExperimentalTypes_UseCurrentConceptNamespaces()
    {
        var experimental = typeof(AlpsGeneticAlgorithm<,,>).Assembly;

        typeof(ZeroObjective).Namespace.ShouldBe("HEAL.HeuristicLib.Objectives");
        typeof(CompositeGenotype<,>).Namespace.ShouldBe("HEAL.HeuristicLib.Encodings.Composite");
        typeof(CompositeSearchSpace<,,,>).Namespace.ShouldBe("HEAL.HeuristicLib.Encodings.Composite");
        typeof(EmptyGenotype).Namespace.ShouldBe("HEAL.HeuristicLib.Encodings.Empty");
        typeof(EmptySearchSpace).Namespace.ShouldBe("HEAL.HeuristicLib.Encodings.Empty");
        experimental.GetType("HEAL.HeuristicLib.Encodings.BoolVectors.BoolVectorNeighborhood`1").ShouldNotBeNull();
        experimental.GetType("HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVectorNeighborhood`1").ShouldNotBeNull();
        experimental.GetType("HEAL.HeuristicLib.Encodings.Permutations.PermutationNeighborhood`1").ShouldNotBeNull();
        experimental.GetType("HEAL.HeuristicLib.Encodings.RealVectors.RealVectorNeighborhood`1").ShouldNotBeNull();
        experimental.GetType("HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions.SymbolicExpressionTree").ShouldNotBeNull();
        experimental.GetType("HEAL.HeuristicLib.Problems.MachineLearning.Legacy.Dataset").ShouldNotBeNull();
        experimental.GetType("HEAL.HeuristicLib.Problems.TravelingSalesman.TravelingSalesmanMoveProblem").ShouldNotBeNull();
    }

    [Fact]
    public void ExperimentalAssembly_DoesNotExposeObsoleteNamespaces()
    {
        string[] obsoletePrefixes =
        [
            "HEAL.HeuristicLib.Genotypes",
            "HEAL.HeuristicLib.Optimization",
            "HEAL.HeuristicLib.Problems.DataAnalysis",
            "HEAL.HeuristicLib.SearchSpaces.Trees"
        ];

        var obsoleteTypes = typeof(AlpsGeneticAlgorithm<,,>).Assembly
            .GetExportedTypes()
            .Where(type => obsoletePrefixes.Any(prefix => type.Namespace?.StartsWith(prefix, StringComparison.Ordinal) == true))
            .Select(type => type.FullName)
            .ToArray();

        obsoleteTypes.ShouldBeEmpty();
    }
}

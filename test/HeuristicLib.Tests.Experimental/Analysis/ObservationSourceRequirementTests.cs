using HEAL.HeuristicLib.Analysis.GenealogyAnalysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

namespace HEAL.HeuristicLib.Tests.Experimental.Analysis;

/// <summary>
/// Covers the rule that an analyzer cannot be built without something to observe.
/// </summary>
public class ObservationSourceRequirementTests
{
    [Fact]
    public void RequiredSources_RejectAnEmptyCollection()
    {
        var exception = Should.Throw<ArgumentException>(() =>
            new ParetoFrontAnalyzer<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(
                SingleObjective.Minimize,
                new ObjectiveVector(1.0),
                []));

        exception.ParamName.ShouldBe("evaluators");
    }

    [Fact]
    public void SingleSource_NeedsNoCollectionCeremony()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 2));
        var algorithm = new GeneticAlgorithm<RealVector>
        {
            PopulationSize = 4,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            Selector = RandomSelector.For(problem)
        };

        var analysis = Analyzer.TraceAverageSimilarity(new NoSimilarity(), algorithm);

        analysis.SampleCount.ShouldBe(0);
    }

    /// <summary>
    /// Genealogy observes three kinds of source and each kind on its own is optional, so the requirement is that the
    /// analyzer has some source rather than one of a particular kind.
    /// </summary>
    [Fact]
    public void GenealogyAnalyzer_NeedsAtLeastOneSourceOfAnyKind()
    {
        var exception = Should.Throw<ArgumentException>(() =>
            new GenealogyAnalyzer<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>());

        exception.Message.ShouldContain("at least one crossover, mutator or algorithm");
    }

    [Fact]
    public void GenealogyAnalyzer_AcceptsAnyOneKindOnItsOwn()
    {
        // Without an interceptor the graph still records descent, it just gains no generational structure.
        var withCrossoverOnly = new GenealogyAnalyzer<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(
            crossovers: [new SinglePointCrossover()]);

        withCrossoverOnly.Graph.ShouldNotBeNull();
    }

    private sealed class NoSimilarity : ICandidateSimilarityCalculator<RealVector>
    {
        public double[,] CalculateSimilarity(IReadOnlyList<EvaluatedCandidate<RealVector>> candidate) =>
            new double[candidate.Count, candidate.Count];
    }
}

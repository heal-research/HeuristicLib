using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Analysis.GenealogyAnalysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

namespace HEAL.HeuristicLib.Tests.Experimental.Analysis;

/// <summary>
/// Covers the rule that an analysis cannot be built without something to observe.
/// </summary>
public class AnchorRequirementTests
{
    [Fact]
    public void RequiredAnchors_RejectAnEmptyCollection()
    {
        var exception = Should.Throw<ArgumentException>(() =>
            new ParetoFrontAnalysis<RealVector, RealVectorSearchSpace, TestFunctionProblem>(
                SingleObjective.Minimize,
                new ObjectiveVector(1.0),
                []));

        exception.ParamName.ShouldBe("evaluators");
    }

    [Fact]
    public void SingleAnchor_NeedsNoCollectionCeremony()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 2));
        var algorithm = new GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>
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
    /// Genealogy observes three kinds of anchor and each kind on its own is optional, so the requirement is that the
    /// analysis has some anchor rather than one of a particular kind.
    /// </summary>
    [Fact]
    public void GenealogyAnalysis_NeedsAtLeastOneAnchorOfAnyKind()
    {
        var exception = Should.Throw<ArgumentException>(() =>
            new GenealogyAnalysis<RealVector, RealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>());

        exception.Message.ShouldContain("at least one crossover, mutator or algorithm");
    }

    [Fact]
    public void GenealogyAnalysis_AcceptsAnyOneKindOnItsOwn()
    {
        // Without an interceptor the graph still records descent, it just gains no generational structure.
        var withCrossoverOnly = new GenealogyAnalysis<RealVector, RealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(
            crossovers: [new SinglePointCrossover()]);

        withCrossoverOnly.Graph.ShouldNotBeNull();
    }

    private sealed class NoSimilarity : ICandidateSimilarityCalculator<RealVector>
    {
        public double[,] CalculateSimilarity(IReadOnlyList<EvaluatedCandidate<RealVector>> candidate) =>
            new double[candidate.Count, candidate.Count];
    }
}

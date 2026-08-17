using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Tests.Algorithms;

/// <summary>
/// Pins how the built-in algorithms place refinement. A refiner runs after creation and after variation, immediately
/// before evaluation, and never runs again on candidates that are carried forward with the objective vectors they
/// already have.
/// </summary>
public class AlgorithmRefinementTests
{
    [Fact]
    public void Refiner_DefaultsToNull()
    {
        var problem = CreateProblem();

        CreateAlgorithm(problem).Refiner.ShouldBeNull();
        CreateHillClimber(problem).Refiner.ShouldBeNull();
    }

    /// <summary>
    /// An unset refiner must leave the search untouched, not merely produce comparable results: the algorithm must
    /// consume randomness identically, so the run is reproducible against a build without refinement.
    /// </summary>
    [Fact]
    public void WithoutARefiner_TheRunIsUnchanged()
    {
        var problem = CreateProblem();

        var withoutRefiner = CreateAlgorithm(problem)
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);
        var withIdentityRefiner = (CreateAlgorithm(problem) with { Refiner = NoChangeRefiner<RealVector>.Instance })
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        withIdentityRefiner.Population.EvaluatedCandidates.Select(candidate => candidate.ObjectiveVector)
            .ShouldBe(withoutRefiner.Population.EvaluatedCandidates.Select(candidate => candidate.ObjectiveVector));
    }

    [Fact]
    public void GeneticAlgorithm_RefinesCreatedAndVariedCandidatesBeforeEvaluation()
    {
        var problem = CreateProblem();
        var refiner = new CountingRefiner();

        (CreateAlgorithm(problem) with { Refiner = refiner })
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        // One batch for the initial population and one for each later generation's offspring.
        refiner.BatchCount.ShouldBe(Generations);
    }

    /// <summary>
    /// The requirement RF-5 exists for. Elites already carry an objective vector, so refining them again would spend
    /// the refinement budget re-doing settled work and would silently change candidates the population has accepted.
    /// </summary>
    [Fact]
    public void GeneticAlgorithm_DoesNotRefineCarriedElites()
    {
        var problem = CreateProblem();
        var refiner = new CountingRefiner();

        (CreateAlgorithm(problem) with { Refiner = refiner, Elites = 2 })
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        // The initial population, then one offspring per later generation: the algorithm selects twice the population
        // size in parents and pairs them, so each generation produces exactly PopulationSize offspring. Two elites per
        // generation would add six more refined candidates if they were passed through the refiner again.
        refiner.RefinedCount.ShouldBe(PopulationSize * Generations);
    }

    [Fact]
    public void GeneticAlgorithm_EvaluatesTheRefinedCandidateRatherThanTheOriginal()
    {
        var problem = CreateProblem();

        var result = (CreateAlgorithm(problem) with { Refiner = new OriginShiftRefiner(), MaximumGenerations = 1 })
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        // The refiner moves every candidate to the origin, where the sphere function is exactly zero. A non-zero
        // objective would mean the algorithm evaluated the candidate it had before refinement.
        result.Population.EvaluatedCandidates.ShouldAllBe(candidate => candidate.ObjectiveVector[0] == 0.0);
    }

    [Fact]
    public void HillClimber_RefinesTheInitialCandidateAndEachNeighborBatch()
    {
        var problem = CreateProblem();
        var refiner = new CountingRefiner();

        (CreateHillClimber(problem) with { Refiner = refiner })
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        refiner.BatchCount.ShouldBeGreaterThan(1);
        refiner.RefinedCount.ShouldBeGreaterThan(1);
    }

    [Fact]
    public void EvolutionStrategy_AndNsga2_AcceptARefiner()
    {
        var problem = CreateProblem();
        var strategyRefiner = new CountingRefiner();
        var nsga2Refiner = new CountingRefiner();

        new EvolutionStrategy<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            PopulationSize = PopulationSize,
            NumberOfChildren = PopulationSize,
            Strategy = EvolutionStrategyType.Plus,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Mutator = new GaussianMutator(0.1, 0.1),
            Crossover = null,
            Selector = RandomSelector.For(problem),
            MaximumGenerations = Generations,
            Refiner = strategyRefiner
        }.Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        new NSGA2<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            PopulationSize = PopulationSize,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            Selector = RandomSelector.For(problem),
            Replacer = ParetoCrowdingReplacer.For(problem, dominateOnEqualities: true),
            MaximumGenerations = Generations,
            Refiner = nsga2Refiner
        }.Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        strategyRefiner.BatchCount.ShouldBe(Generations);
        nsga2Refiner.BatchCount.ShouldBe(Generations);
    }

    private const int PopulationSize = 5;
    private const int Generations = 4;

    private static TestFunctionProblem CreateProblem() => new(new SphereFunction(dimension: 3));

    private static GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateAlgorithm(TestFunctionProblem problem) =>
        new()
        {
            PopulationSize = PopulationSize,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            MutationRate = 0.5,
            Selector = RandomSelector.For(problem),
            Elites = 0,
            MaximumGenerations = Generations
        };

    private static HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateHillClimber(TestFunctionProblem problem) =>
        new()
        {
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Mutator = new GaussianMutator(0.1, 0.1),
            Direction = LocalSearchDirection.BestImprovement,
            MaxNeighbors = 4,
            BatchSize = 2
        };

    /// <summary>Records how often refinement ran and how many candidates it saw, without changing them.</summary>
    private sealed record CountingRefiner : Refiner<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        private readonly Counter counter = new();

        public int BatchCount => counter.Batches;
        public int RefinedCount => counter.Candidates;

        public override IRefinerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
            new Instance(counter);

        private sealed class Counter
        {
            public int Batches;
            public int Candidates;
        }

        private sealed class Instance(Counter counter) : IRefinerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            public IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                counter.Batches++;
                counter.Candidates += candidates.Count;
                return candidates;
            }
        }
    }

    /// <summary>Moves every candidate to the origin, which is the sphere function's optimum.</summary>
    private sealed record OriginShiftRefiner : SingleCandidateRefiner<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector RefineCandidate(RealVector candidate, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            RealVector.Repeat(0.0, candidate.Count);
    }
}

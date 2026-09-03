using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using SinglePointCrossover = HEAL.HeuristicLib.Encodings.RealVectors.SinglePointCrossover;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Algorithms;

public class AlgorithmRefinementTests
{
    [Fact]
    public void Refiner_DefaultsToNull()
    {
        var problem = CreateProblem();

        CreateAlgorithm(problem).Refiner.ShouldBeNull();
        CreateHillClimber(problem).Refiner.ShouldBeNull();
    }

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

        // The refiner moves every candidate to the origin, where the sphere function is exactly zero.
        foreach (var candidate in result.Population.EvaluatedCandidates)
        {
            candidate.ObjectiveVector[0].ShouldBe(0.0);
        }
    }

    // The algorithm has no positional requirement on the production path; the replacer decides the surviving population.
    [Fact]
    public void GeneticAlgorithm_WithARefinerThatResizesThePopulation_ContinuesWithTheReturnedPopulation()
    {
        var problem = CreateProblem();

        var result = (CreateAlgorithm(problem) with { Refiner = new DropLastRefiner() })
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        result.Population.EvaluatedCandidates.Count.ShouldBe(PopulationSize - 1);
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

    // The Baldwinian counterpart of the refiner slot: the population keeps the produced candidates while their
    // objective vectors come from transient refined copies.
    [Fact]
    public void GeneticAlgorithm_WithARefiningEvaluator_KeepsTheOriginalCandidatesAndTheRefinedObjectiveVectors()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            Evaluator = new ProblemEvaluator<RealVector>().WithRefinement(new OriginShiftRefiner()),
            MaximumGenerations = 1
        };

        var result = algorithm.Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        foreach (var candidate in result.Population.EvaluatedCandidates)
        {
            // The refined copy sits at the origin, where the sphere function is zero.
            candidate.ObjectiveVector[0].ShouldBe(0.0);
            // The population itself was never moved there.
            candidate.Candidate.ShouldNotBe(RealVector.Repeat(0.0, candidate.Candidate.Count));
        }
    }

    [Fact]
    public void EvolutionStrategy_AndNsga2_AcceptARefiner()
    {
        var problem = CreateProblem();
        var strategyRefiner = new CountingRefiner();
        var nsga2Refiner = new CountingRefiner();

        new EvolutionStrategy<RealVector>
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

        new NSGA2<RealVector>
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

    [Fact]
    public void AlpsGeneticAlgorithm_RefinesTheInitialPopulationAndEachOffspringBatch()
    {
        var problem = CreateProblem();
        var refiner = new CountingRefiner();

        (CreateAlps(problem) with { Refiner = refiner })
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        refiner.BatchCount.ShouldBe(Generations);
    }

    // ALPS carries its elites from the previous layer population, which never passes the production path again.
    [Fact]
    public void AlpsGeneticAlgorithm_DoesNotRefineCarriedElites()
    {
        var problem = CreateProblem();
        var refiner = new CountingRefiner();

        (CreateAlps(problem) with { Refiner = refiner, Elites = 2 })
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        refiner.RefinedCount.ShouldBe(PopulationSize * Generations);
    }

    // The plus strategy carries the parents into the next generation, so only the children are produced and refined.
    [Fact]
    public void EvolutionStrategy_WithThePlusStrategy_DoesNotRefineCarriedParents()
    {
        var problem = CreateProblem();
        var refiner = new CountingRefiner();

        (CreateEvolutionStrategy(problem) with { Refiner = refiner })
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        refiner.RefinedCount.ShouldBe(PopulationSize + (Children * (Generations - 1)));
    }

    // The repopulation branch is the one an ordinary run never reaches. A refiner that keeps every offspring from
    // dominating its parents empties the population and forces that branch on the following generation.
    [Fact]
    public void OpenEndedGeneticAlgorithm_RefinesAtEveryProductionSiteIncludingRepopulation()
    {
        var problem = CreateProblem();
        var refiner = new CountingRefiner();
        var algorithm = new OpenEndedRelevantAllelesPreservingGeneticAlgorithm<RealVector>
        {
            PopulationSize = PopulationSize,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            Selector = RandomSelector.For(problem),
            Elites = 0,
            MaxEffort = PopulationSize,
            MaximumGenerations = Generations,
            Refiner = PipelineRefiner.Create(new FarFromOriginRefiner(), refiner)
        };

        var populationSizes = new List<int>();
        foreach (var state in algorithm.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken))
        {
            populationSizes.Add(state.Population.EvaluatedCandidates.Count);
        }

        // The run has to visit an empty population followed by a repopulated one, or it never reaches that branch.
        var emptyIndex = populationSizes.IndexOf(0);
        emptyIndex.ShouldBeGreaterThanOrEqualTo(0);
        populationSizes[emptyIndex + 1].ShouldBe(PopulationSize);

        // One refined batch per generation, including the repopulating one.
        refiner.BatchCount.ShouldBe(populationSizes.Count);
    }

    // Refinement must not consume randomness when no refiner is configured, or adding the setting would have changed
    // every existing run.
    [Fact]
    public void WithoutARefiner_EveryAlgorithmRunsExactlyAsItDoesWithAnIdentityRefiner()
    {
        var problem = CreateProblem();

        foreach (var (name, run) in AllAlgorithms(problem))
        {
            run(null).ShouldBe(run(NoChangeRefiner<RealVector>.Instance), name);
        }
    }

    private static IEnumerable<(string Name, Func<IRefiner<RealVector>?, IReadOnlyList<ObjectiveVector>> Run)> AllAlgorithms(TestFunctionProblem problem)
    {
        yield return ("genetic algorithm", refiner => ObjectiveVectorsOf((CreateAlgorithm(problem) with { Refiner = refiner })
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Population));
        yield return ("hill climber", refiner => ObjectiveVectorsOf((CreateHillClimber(problem) with { Refiner = refiner })
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Population));
        yield return ("evolution strategy", refiner => ObjectiveVectorsOf((CreateEvolutionStrategy(problem) with { Refiner = refiner })
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Population));
        yield return ("nsga2", refiner => ObjectiveVectorsOf((CreateNsga2(problem) with { Refiner = refiner })
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Population));
        yield return ("alps", refiner => ObjectiveVectorsOf((CreateAlps(problem) with { Refiner = refiner })
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Population.Single()));
        yield return ("open ended", refiner => ObjectiveVectorsOf((CreateOpenEnded(problem) with { Refiner = refiner })
            .Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Population));
    }

    private static IReadOnlyList<ObjectiveVector> ObjectiveVectorsOf(Population<RealVector> population) =>
        [.. population.EvaluatedCandidates.Select(candidate => candidate.ObjectiveVector)];

    private const int PopulationSize = 5;
    private const int Children = 4;
    private const int Generations = 4;

    private static TestFunctionProblem CreateProblem() => new(new SphereFunction(dimension: 3));

    private static GeneticAlgorithm<RealVector> CreateAlgorithm(TestFunctionProblem problem) =>
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

    private static HillClimber<RealVector> CreateHillClimber(TestFunctionProblem problem) =>
        new()
        {
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Mutator = new GaussianMutator(0.1, 0.1),
            Direction = LocalSearchDirection.BestImprovement,
            MaxNeighbors = 4,
            BatchSize = 2
        };

    private sealed record CountingRefiner : Refiner<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        private readonly Counter counter = new();

        public int BatchCount => counter.Batches;
        public int RefinedCount => counter.Candidates;

        public override IRefinerInstance<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
            new Instance(counter);

        private sealed class Counter
        {
            public int Batches;
            public int Candidates;
        }

        private sealed class Instance(Counter counter) : IRefinerInstance<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                counter.Batches++;
                counter.Candidates += candidates.Count;
                return candidates;
            }
        }
    }

    private static EvolutionStrategy<RealVector> CreateEvolutionStrategy(TestFunctionProblem problem) =>
        new()
        {
            PopulationSize = PopulationSize,
            NumberOfChildren = Children,
            Strategy = EvolutionStrategyType.Plus,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Mutator = new GaussianMutator(0.1, 0.1),
            Crossover = null,
            Selector = RandomSelector.For(problem),
            MaximumGenerations = Generations
        };

    private static NSGA2<RealVector> CreateNsga2(TestFunctionProblem problem) =>
        new()
        {
            PopulationSize = PopulationSize,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            Selector = RandomSelector.For(problem),
            Replacer = ParetoCrowdingReplacer.For(problem, dominateOnEqualities: true),
            MaximumGenerations = Generations
        };

    private static AlpsGeneticAlgorithm<RealVector> CreateAlps(TestFunctionProblem problem) =>
        new()
        {
            PopulationSize = PopulationSize,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            MutationRate = 0.5,
            Selector = RandomSelector.For(problem),
            MaximumGenerations = Generations
        };

    private static OpenEndedRelevantAllelesPreservingGeneticAlgorithm<RealVector> CreateOpenEnded(TestFunctionProblem problem) =>
        new()
        {
            PopulationSize = PopulationSize,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            Selector = RandomSelector.For(problem),
            MaxEffort = PopulationSize,
            MaximumGenerations = Generations
        };

    // Moves every candidate far away from the optimum, so no offspring can dominate the parents it came from.
    private sealed record FarFromOriginRefiner : SingleCandidateRefiner<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector RefineCandidate(RealVector candidate, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            RealVector.Repeat(searchSpace.GetMaximum(0), candidate.Count);
    }

    private sealed record DropLastRefiner : StatelessRefiner<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            [.. candidates.Take(Math.Max(0, candidates.Count - 1))];
    }

    private sealed record OriginShiftRefiner : SingleCandidateRefiner<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector RefineCandidate(RealVector candidate, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            RealVector.Repeat(0.0, candidate.Count);
    }
}

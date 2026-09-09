using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record AlpsState<TCandidate> : SearchState
{
    public required ImmutableArray<Population<TCandidate>> Population { get; init; }
    public required ImmutableArray<ImmutableArray<int>> Ages { get; init; }
}

public record AlpsGeneticAlgorithm<TCandidate>
    : IterativeAlgorithm<AlpsGeneticAlgorithm<TCandidate>, TCandidate, AlpsState<TCandidate>>
{
    public required int PopulationSize { get; init; }
    public required ICreator<TCandidate> Creator { get; init; }
    public required ICrossover<TCandidate> Crossover { get; init; }
    public required IMutator<TCandidate> Mutator { get; init; }
    public required ISelector<TCandidate> Selector { get; init; }
    public IEvaluator<TCandidate> Evaluator { get; init; } = new ProblemEvaluator<TCandidate>();

    public int Elites { get; init; }
    public IRefiner<TCandidate>? Refiner { get; init; }

    /// <summary>
    /// Gets the generation limit, or <see langword="null"/> for no limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit completes before the first generation is produced.</remarks>
    public int? MaximumGenerations { get; init; }

    /// <summary>
    /// Gets the probability that an offspring is mutated. The expected value is in <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The rate is applied as a threshold against a random value in <c>[0, 1)</c>. A value at most zero, negative
    /// infinity and <c>NaN</c> never mutate; a value at least one and positive infinity always mutate.
    /// </remarks>
    public double MutationRate { get; init; } = 0.1;

    protected override IterativeAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, AlpsState<TCandidate>> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry, IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, AlpsState<TCandidate>>? resolvedInterceptor)
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem>();
        var effectiveMutator = MutationRate >= 1.0 ? Mutator : Mutator.AppliedAtRate(MutationRate);
        return new Instance<TRunSearchSpace, TRunProblem>(resolvedInterceptor, resolver.Resolve(Evaluator), resolver.Resolve(Creator), resolver.Resolve(Crossover),
            resolver.Resolve(effectiveMutator), resolver.Resolve(Selector), resolver.ResolveOptional(Refiner), PopulationSize, Elites, MaximumGenerations);
    }

    private sealed class Instance<TSearchSpace, TProblem>(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, AlpsState<TCandidate>>? interceptor,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        ICrossoverInstance<TCandidate, TSearchSpace, TProblem> crossover,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorInstance<TCandidate, TSearchSpace, TProblem> selector,
        IRefinerInstance<TCandidate, TSearchSpace, TProblem>? refiner,
        int populationSize,
        int elites,
        int? maximumGenerations)
        : IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, AlpsState<TCandidate>>(interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        protected override bool HasCompleted(int yieldedStateCount, AlpsState<TCandidate>? previousState, TProblem problem) =>
            maximumGenerations is not null && yieldedStateCount >= maximumGenerations.Value;

        protected override AlpsState<TCandidate> ExecuteStep(AlpsState<TCandidate>? previousState, TProblem problem, IRandomNumberGenerator random)
        {
            var searchSpace = problem.SearchSpace;

            if (previousState is null)
            {
                var initialLayerPopulation = creator.Create(populationSize, random, searchSpace, problem);
                if (refiner is not null)
                {
                    initialLayerPopulation = refiner.Refine(initialLayerPopulation, random, searchSpace, problem);
                }

                var initialPopulation = initialLayerPopulation.ToEvaluated(evaluator.Evaluate(initialLayerPopulation, random, searchSpace, problem));
                return new()
                {
                    Population = [Population.From(initialPopulation)],
                    Ages = [Enumerable.Repeat(0, populationSize).ToImmutableArray()]
                };
            }

            var offspringCount = populationSize;
            var oldPopulation = previousState.Population[0].ToArray();
            var selectedParents = selector.Select(oldPopulation, problem.Objective, offspringCount * 2, random, searchSpace, problem);
            var parentPairs = new Parents<TCandidate>[offspringCount];
            var offspringAges = new int[offspringCount];
            var nextAge = previousState.Ages[0].DefaultIfEmpty(0).Max() + 1;
            for (int i = 0, j = 0; i < offspringCount; i++, j += 2)
            {
                parentPairs[i] = Parents.From(selectedParents[j].Candidate, selectedParents[j + 1].Candidate);
                offspringAges[i] = nextAge;
            }

            var offspring = crossover.Cross(parentPairs, random, searchSpace, problem);
            offspring = mutator.Mutate(offspring, random, searchSpace, problem);
            if (refiner is not null)
            {
                offspring = refiner.Refine(offspring, random, searchSpace, problem);
            }

            var offspringPopulation = offspring.ToEvaluated(evaluator.Evaluate(offspring, random, searchSpace, problem));
            var newPopulation = ElitismReplacer.Replace(oldPopulation, offspringPopulation, problem.Objective, offspringCount, elites);

            return new()
            {
                Population = [Population.From(newPopulation)],
                Ages = [offspringAges.ToImmutableArray()]
            };
        }
    }
}

using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record AlpsState<TCandidate> : SearchState
{
    public required ImmutableArray<Population<TCandidate>> Population { get; init; }
    public required ImmutableArray<ImmutableArray<int>> Ages { get; init; }
}

public record AlpsGeneticAlgorithm<TCandidate, TSearchSpace, TProblem>
    : IterativeAlgorithm<AlpsGeneticAlgorithm<TCandidate, TSearchSpace, TProblem>, TCandidate, TSearchSpace, TProblem, AlpsState<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public required int PopulationSize { get; init; }
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem> Crossover { get; init; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
    public required ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; init; }
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; } = new DirectEvaluator<TCandidate>();

    public int Elites { get; init; }
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

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, AlpsState<TCandidate>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, AlpsState<TCandidate>>? resolvedInterceptor)
    {
        var effectiveMutator = MutationRate >= 1.0 ? Mutator : Mutator.WithRate(MutationRate);
        return new Instance(resolvedInterceptor, instanceRegistry.Resolve(Evaluator), instanceRegistry.Resolve(Creator), instanceRegistry.Resolve(Crossover),
            instanceRegistry.Resolve(effectiveMutator), instanceRegistry.Resolve(Selector), PopulationSize, Elites, MaximumGenerations);
    }

    private sealed class Instance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, AlpsState<TCandidate>>? interceptor,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        ICrossoverInstance<TCandidate, TSearchSpace, TProblem> crossover,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorInstance<TCandidate, TSearchSpace, TProblem> selector,
        int populationSize,
        int elites,
        int? maximumGenerations)
        : IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, AlpsState<TCandidate>>(interceptor)
    {
        protected override bool HasCompleted(int yieldedStateCount, AlpsState<TCandidate>? previousState, TProblem problem) =>
            maximumGenerations is not null && yieldedStateCount >= maximumGenerations.Value;

        protected override AlpsState<TCandidate> ExecuteStep(AlpsState<TCandidate>? previousState, TProblem problem, IRandomNumberGenerator random)
        {
            var searchSpace = problem.SearchSpace;

            if (previousState is null)
            {
                var initialLayerPopulation = creator.Create(populationSize, random, searchSpace, problem);
                var initialFitnesses = evaluator.Evaluate(initialLayerPopulation, random, searchSpace, problem);
                return new()
                {
                    Population = [Population.From(initialLayerPopulation, initialFitnesses)],
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
            var fitnesses = evaluator.Evaluate(offspring, random, searchSpace, problem);
            var offspringPopulation = Population.From(offspring, fitnesses).EvaluatedCandidates;
            var newPopulation = ElitismReplacer.Replace(oldPopulation, offspringPopulation, problem.Objective, offspringCount, elites);

            return new()
            {
                Population = [Population.From(newPopulation)],
                Ages = [offspringAges.ToImmutableArray()]
            };
        }
    }
}

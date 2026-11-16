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
    public required IOperator<Creation, TCandidate> Creator { get; init; }
    public required IOperator<Crossover, TCandidate> Crossover { get; init; }
    public required IOperator<Mutation, TCandidate> Mutator { get; init; }
    public required IOperator<Selection, TCandidate> Selector { get; init; }
    public IOperator<Evaluation, TCandidate> Evaluator { get; init; } = new ProblemEvaluator<TCandidate>();

    public int Elites { get; init; }
    public IOperator<Refinement, TCandidate>? Refiner { get; init; }
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

    public static Parameter<AlpsGeneticAlgorithm<TCandidate>, int> PopulationSizeParameter { get; } =
        new(nameof(PopulationSize), static algorithm => algorithm.PopulationSize, static (algorithm, value) => algorithm with { PopulationSize = value });

    public static Parameter<AlpsGeneticAlgorithm<TCandidate>, IOperator<Creation, TCandidate>> CreatorParameter { get; } =
        new(nameof(Creator), static algorithm => algorithm.Creator, static (algorithm, value) => algorithm with { Creator = value });

    public static Parameter<AlpsGeneticAlgorithm<TCandidate>, IOperator<Crossover, TCandidate>> CrossoverParameter { get; } =
        new(nameof(Crossover), static algorithm => algorithm.Crossover, static (algorithm, value) => algorithm with { Crossover = value });

    public static Parameter<AlpsGeneticAlgorithm<TCandidate>, IOperator<Mutation, TCandidate>> MutatorParameter { get; } =
        new(nameof(Mutator), static algorithm => algorithm.Mutator, static (algorithm, value) => algorithm with { Mutator = value });

    public static Parameter<AlpsGeneticAlgorithm<TCandidate>, IOperator<Selection, TCandidate>> SelectorParameter { get; } =
        new(nameof(Selector), static algorithm => algorithm.Selector, static (algorithm, value) => algorithm with { Selector = value });

    public static Parameter<AlpsGeneticAlgorithm<TCandidate>, IOperator<Evaluation, TCandidate>> EvaluatorParameter { get; } =
        new(nameof(Evaluator), static algorithm => algorithm.Evaluator, static (algorithm, value) => algorithm with { Evaluator = value });

    public static Parameter<AlpsGeneticAlgorithm<TCandidate>, int> ElitesParameter { get; } =
        new(nameof(Elites), static algorithm => algorithm.Elites, static (algorithm, value) => algorithm with { Elites = value });

    public static Parameter<AlpsGeneticAlgorithm<TCandidate>, IOperator<Refinement, TCandidate>?> RefinerParameter { get; } =
        new(nameof(Refiner), static algorithm => algorithm.Refiner, static (algorithm, value) => algorithm with { Refiner = value });

    public static Parameter<AlpsGeneticAlgorithm<TCandidate>, int?> MaximumGenerationsParameter { get; } =
        new(nameof(MaximumGenerations), static algorithm => algorithm.MaximumGenerations, static (algorithm, value) => algorithm with { MaximumGenerations = value });

    public static Parameter<AlpsGeneticAlgorithm<TCandidate>, double> MutationRateParameter { get; } =
        new(nameof(MutationRate), static algorithm => algorithm.MutationRate, static (algorithm, value) => algorithm with { MutationRate = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [PopulationSizeParameter, CreatorParameter, CrossoverParameter, MutatorParameter, SelectorParameter, EvaluatorParameter, ElitesParameter, RefinerParameter, MaximumGenerationsParameter, MutationRateParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IterativeAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, AlpsState<TCandidate>> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, AlpsState<TCandidate>> scope)
    {
        var interceptor = scope.ResolveOptional(Interceptor);
        var evaluator = scope.Resolve(Evaluator);
        var creator = scope.Resolve(Creator);
        var crossover = scope.Resolve(Crossover);
        var mutator = scope.Resolve(Mutator);
        var selector = scope.Resolve(Selector);
        var refiner = scope.ResolveOptional(Refiner);
        return new Executor<TSearchSpace, TProblem>(interceptor, evaluator, creator, crossover, mutator, selector, refiner,
            PopulationSize, Elites, MaximumGenerations, MutationRate, scope.Problem);
    }

    private sealed class Executor<TSearchSpace, TProblem>(
        IInterceptionExecutor<AlpsState<TCandidate>>? interceptor,
        IEvaluationExecutor<TCandidate> evaluator,
        ICreationExecutor<TCandidate> creator,
        ICrossoverExecutor<TCandidate> crossover,
        IMutationExecutor<TCandidate> mutator,
        ISelectionExecutor<TCandidate> selector,
        IRefinementExecutor<TCandidate>? refiner,
        int populationSize,
        int elites,
        int? maximumGenerations,
        double mutationRate, TProblem problem)
        : IterativeAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, AlpsState<TCandidate>>(interceptor, problem)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly WeightedRouter<IMutationExecutor<TCandidate>?> mutationRouter = WeightedRouter.AtRate(mutator, mutationRate);

        protected override bool HasCompleted(int yieldedStateCount, AlpsState<TCandidate>? previousState) =>
            maximumGenerations is not null && yieldedStateCount >= maximumGenerations.Value;

        protected override AlpsState<TCandidate> ExecuteStep(AlpsState<TCandidate>? previousState, IRandomNumberGenerator random)
        {

            if (previousState is null)
            {
                var initialLayerPopulation = creator.Create(populationSize, random);
                if (refiner is not null)
                {
                    initialLayerPopulation = refiner.Refine(initialLayerPopulation, random);
                }

                var initialPopulation = initialLayerPopulation.ToEvaluated(evaluator.Evaluate(initialLayerPopulation, random));
                return new()
                {
                    Population = [Population.From(initialPopulation)],
                    Ages = [Enumerable.Repeat(0, populationSize).ToImmutableArray()]
                };
            }

            var offspringCount = populationSize;
            var oldPopulation = previousState.Population[0].ToArray();
            var selectedParents = selector.Select(oldPopulation, Problem.Objective, offspringCount * 2, random);
            var parentPairs = new Parents<TCandidate>[offspringCount];
            var offspringAges = new int[offspringCount];
            var nextAge = previousState.Ages[0].DefaultIfEmpty(0).Max() + 1;
            for (int i = 0, j = 0; i < offspringCount; i++, j += 2)
            {
                parentPairs[i] = Parents.From(selectedParents[j].Candidate, selectedParents[j + 1].Candidate);
                offspringAges[i] = nextAge;
            }

            var offspring = crossover.Cross(parentPairs, random);
            offspring = mutationRouter.Route(offspring, random, (selected, batch) =>
                selected is null ? batch : selected.Mutate(batch, random));
            if (refiner is not null)
            {
                offspring = refiner.Refine(offspring, random);
            }

            var offspringPopulation = offspring.ToEvaluated(evaluator.Evaluate(offspring, random));
            var newPopulation = ElitismReplacer.Replace(oldPopulation, offspringPopulation, Problem.Objective, offspringCount, elites);

            return new()
            {
                Population = [Population.From(newPopulation)],
                Ages = [offspringAges.ToImmutableArray()]
            };
        }
    }
}

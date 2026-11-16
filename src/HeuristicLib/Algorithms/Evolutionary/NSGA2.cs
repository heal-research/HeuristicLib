using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

#pragma warning disable S101
public record NSGA2<TCandidate>
#pragma warning restore S101
    : IterativeAlgorithm<NSGA2<TCandidate>, TCandidate, PopulationState<TCandidate>>
{
    public int PopulationSize { get; init; } = NSGA2Defaults.PopulationSize;
    public required IOperator<Creation, TCandidate> Creator { get; init; }
    public required IOperator<Crossover, TCandidate> Crossover { get; init; }
    public required IOperator<Mutation, TCandidate> Mutator { get; init; }
    public IOperator<Selection, TCandidate> Selector { get; init; } = NSGA2Defaults.Selector<TCandidate>();
    public IOperator<Replacement, TCandidate> Replacer { get; init; } = NSGA2Defaults.Replacer<TCandidate>();
    public IOperator<Evaluation, TCandidate> Evaluator { get; init; } = NSGA2Defaults.Evaluator<TCandidate>();

    /// <summary>
    /// Gets the probability that an offspring is mutated. The expected value is in <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The rate is applied as a threshold against a random value in <c>[0, 1)</c>. A value at most zero, negative
    /// infinity and <c>NaN</c> never mutate; a value at least one and positive infinity always mutate.
    /// </remarks>
    public double MutationRate { get; init; } = NSGA2Defaults.MutationRate;
    public IOperator<Refinement, TCandidate>? Refiner { get; init; }
    /// <summary>
    /// Gets the generation limit, or <see langword="null"/> for no limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit completes before the first generation is produced.</remarks>
    public int? MaximumGenerations { get; init; } = NSGA2Defaults.MaximumGenerations;

    public static Parameter<NSGA2<TCandidate>, int> PopulationSizeParameter { get; } =
        new(nameof(PopulationSize), static a2 => a2.PopulationSize, static (a2, value) => a2 with { PopulationSize = value });

    public static Parameter<NSGA2<TCandidate>, IOperator<Creation, TCandidate>> CreatorParameter { get; } =
        new(nameof(Creator), static a2 => a2.Creator, static (a2, value) => a2 with { Creator = value });

    public static Parameter<NSGA2<TCandidate>, IOperator<Crossover, TCandidate>> CrossoverParameter { get; } =
        new(nameof(Crossover), static a2 => a2.Crossover, static (a2, value) => a2 with { Crossover = value });

    public static Parameter<NSGA2<TCandidate>, IOperator<Mutation, TCandidate>> MutatorParameter { get; } =
        new(nameof(Mutator), static a2 => a2.Mutator, static (a2, value) => a2 with { Mutator = value });

    public static Parameter<NSGA2<TCandidate>, IOperator<Selection, TCandidate>> SelectorParameter { get; } =
        new(nameof(Selector), static a2 => a2.Selector, static (a2, value) => a2 with { Selector = value });

    public static Parameter<NSGA2<TCandidate>, IOperator<Replacement, TCandidate>> ReplacerParameter { get; } =
        new(nameof(Replacer), static a2 => a2.Replacer, static (a2, value) => a2 with { Replacer = value });

    public static Parameter<NSGA2<TCandidate>, IOperator<Evaluation, TCandidate>> EvaluatorParameter { get; } =
        new(nameof(Evaluator), static a2 => a2.Evaluator, static (a2, value) => a2 with { Evaluator = value });

    public static Parameter<NSGA2<TCandidate>, double> MutationRateParameter { get; } =
        new(nameof(MutationRate), static a2 => a2.MutationRate, static (a2, value) => a2 with { MutationRate = value });

    public static Parameter<NSGA2<TCandidate>, IOperator<Refinement, TCandidate>?> RefinerParameter { get; } =
        new(nameof(Refiner), static a2 => a2.Refiner, static (a2, value) => a2 with { Refiner = value });

    public static Parameter<NSGA2<TCandidate>, int?> MaximumGenerationsParameter { get; } =
        new(nameof(MaximumGenerations), static a2 => a2.MaximumGenerations, static (a2, value) => a2 with { MaximumGenerations = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [PopulationSizeParameter, CreatorParameter, CrossoverParameter, MutatorParameter, SelectorParameter, ReplacerParameter, EvaluatorParameter, MutationRateParameter, RefinerParameter, MaximumGenerationsParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IterativeAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> scope)
    {
        var interceptor = scope.ResolveOptional(Interceptor);
        var evaluator = scope.Resolve(Evaluator);
        var creator = scope.Resolve(Creator);
        var crossover = scope.Resolve(Crossover);
        var mutator = scope.Resolve(Mutator);
        var selector = scope.Resolve(Selector);
        var replacer = scope.Resolve(Replacer);
        var refiner = scope.ResolveOptional(Refiner);
        return new Executor<TSearchSpace, TProblem>(interceptor, evaluator, creator, crossover, mutator, selector, replacer, refiner,
            PopulationSize, MaximumGenerations, MutationRate, scope.Problem);
    }

    private sealed class Executor<TSearchSpace, TProblem>(
        IInterceptionExecutor<PopulationState<TCandidate>>? interceptor,
        IEvaluationExecutor<TCandidate> evaluator,
        ICreationExecutor<TCandidate> creator,
        ICrossoverExecutor<TCandidate> crossover,
        IMutationExecutor<TCandidate> mutator,
        ISelectionExecutor<TCandidate> selector,
        IReplacementExecutor<TCandidate> replacer,
        IRefinementExecutor<TCandidate>? refiner,
        int populationSize,
        int? maximumGenerations,
        double mutationRate, TProblem problem)
        : IterativeAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>(interceptor, problem)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly WeightedRouter<IMutationExecutor<TCandidate>?> mutationRouter = WeightedRouter.AtRate(mutator, mutationRate);

        protected override bool HasCompleted(int yieldedStateCount, PopulationState<TCandidate>? previousState) =>
            maximumGenerations is not null && yieldedStateCount >= maximumGenerations.Value;

        protected override PopulationState<TCandidate> ExecuteStep(PopulationState<TCandidate>? previousState, IRandomNumberGenerator random)
        {
            if (previousState is null)
            {
                var initialSolutions = creator.Create(populationSize, random);
                if (refiner is not null)
                {
                    initialSolutions = refiner.Refine(initialSolutions, random);
                }

                var initialPopulation = initialSolutions.ToEvaluated(evaluator.Evaluate(initialSolutions, random));
                return Population.From(initialPopulation).ToPopulationState();
            }

            var parents = selector.Select(previousState.Population.EvaluatedCandidates, Problem.Objective, populationSize * 2, random).ToParents(Problem.Objective);
            var children = crossover.Cross(parents, random);
            var mutants = mutationRouter.Route(children, random, (selected, batch) =>
                selected is null ? batch : selected.Mutate(batch, random));
            if (refiner is not null)
            {
                mutants = refiner.Refine(mutants, random);
            }

            var newPopulation = mutants.ToEvaluated(evaluator.Evaluate(mutants, random));
            var nextPopulation = replacer.Replace(previousState.Population.EvaluatedCandidates, newPopulation, Problem.Objective, populationSize, random);

            return Population.From(nextPopulation).ToPopulationState();
        }
    }
}

#pragma warning disable S101
public static class NSGA2
#pragma warning restore S101
{
    /// <summary>
    /// Creates an NSGA-II for a problem, asking the problem first and its search space second for every required
    /// operator the caller does not supply.
    /// </summary>
    /// <remarks>
    /// The creator, crossover and mutator are selected independently. An explicit argument wins, followed by a
    /// problem recommendation and then a search space recommendation. The selector, replacer and evaluator come from
    /// <see cref="NSGA2Defaults"/> when omitted. Other omitted operators remain null.
    /// </remarks>
    /// <exception cref="InvalidOperationException">No value or recommendation is available for one or more required operators.</exception>
    public static NSGA2<TCandidate> For<TProblem, TCandidate, TSearchSpace>(
        Problem<TProblem, TCandidate, TSearchSpace> problem,
        IOperator<Creation, TCandidate>? creator = null,
        IOperator<Crossover, TCandidate>? crossover = null,
        IOperator<Mutation, TCandidate>? mutator = null,
        IOperator<Selection, TCandidate>? selector = null,
        IOperator<Replacement, TCandidate>? replacer = null,
        IOperator<Evaluation, TCandidate>? evaluator = null,
        IOperator<Refinement, TCandidate>? refiner = null,
        IOperator<Interception<PopulationState<TCandidate>>, TCandidate>? interceptor = null,
        int populationSize = NSGA2Defaults.PopulationSize,
        int? maximumGenerations = NSGA2Defaults.MaximumGenerations,
        double mutationRate = NSGA2Defaults.MutationRate)
        where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
        where TSearchSpace : class, ISearchSpace<TCandidate>
    {
        var searchSpace = problem.SearchSpace;
        var recommendations = new OperatorRecommendationResolution(problem, searchSpace);
        creator = recommendations.GetOrRecommend(nameof(creator), creator);
        crossover = recommendations.GetOrRecommend(nameof(crossover), crossover);
        mutator = recommendations.GetOrRecommend(nameof(mutator), mutator);
        recommendations.ThrowIfIncomplete(nameof(NSGA2));

        return new()
        {
            Creator = creator!,
            Crossover = crossover!,
            Mutator = mutator!,
            Selector = selector ?? NSGA2Defaults.Selector<TCandidate>(),
            Replacer = replacer ?? NSGA2Defaults.Replacer<TCandidate>(),
            Evaluator = evaluator ?? NSGA2Defaults.Evaluator<TCandidate>(),
            Refiner = refiner,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            MaximumGenerations = maximumGenerations,
            MutationRate = mutationRate
        };
    }

    /// <summary>
    /// Creates an NSGA-II from a search space's operator recommendations alone, with no problem instance.
    /// </summary>
    /// <remarks>
    /// The creator, crossover and mutator are selected independently. An explicit argument wins over the search
    /// space recommendation. The selector, replacer and evaluator come from <see cref="NSGA2Defaults"/> when omitted.
    /// Other omitted operators remain null.
    /// </remarks>
    /// <exception cref="InvalidOperationException">No value or recommendation is available for one or more required operators.</exception>
    public static NSGA2<TCandidate> For<TCandidate>(
        ISearchSpace<TCandidate> searchSpace,
        IOperator<Creation, TCandidate>? creator = null,
        IOperator<Crossover, TCandidate>? crossover = null,
        IOperator<Mutation, TCandidate>? mutator = null,
        IOperator<Selection, TCandidate>? selector = null,
        IOperator<Replacement, TCandidate>? replacer = null,
        IOperator<Evaluation, TCandidate>? evaluator = null,
        IOperator<Refinement, TCandidate>? refiner = null,
        IOperator<Interception<PopulationState<TCandidate>>, TCandidate>? interceptor = null,
        int populationSize = NSGA2Defaults.PopulationSize,
        int? maximumGenerations = NSGA2Defaults.MaximumGenerations,
        double mutationRate = NSGA2Defaults.MutationRate)
    {
        var recommendations = new OperatorRecommendationResolution(problem: null, searchSpace);
        creator = recommendations.GetOrRecommend(nameof(creator), creator);
        crossover = recommendations.GetOrRecommend(nameof(crossover), crossover);
        mutator = recommendations.GetOrRecommend(nameof(mutator), mutator);
        recommendations.ThrowIfIncomplete(nameof(NSGA2));

        return new()
        {
            Creator = creator!,
            Crossover = crossover!,
            Mutator = mutator!,
            Selector = selector ?? NSGA2Defaults.Selector<TCandidate>(),
            Replacer = replacer ?? NSGA2Defaults.Replacer<TCandidate>(),
            Evaluator = evaluator ?? NSGA2Defaults.Evaluator<TCandidate>(),
            Refiner = refiner,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            MaximumGenerations = maximumGenerations,
            MutationRate = mutationRate
        };
    }

    /// <summary>
    /// Creates an NSGA-II from the operators it requires, inferring the candidate type from them. Every remaining
    /// member is optional and falls back to <see cref="NSGA2Defaults"/>.
    /// </summary>
    public static NSGA2<TCandidate> Create<TCandidate>(
        IOperator<Creation, TCandidate> creator,
        IOperator<Crossover, TCandidate> crossover,
        IOperator<Mutation, TCandidate> mutator,
        IOperator<Selection, TCandidate>? selector = null,
        IOperator<Replacement, TCandidate>? replacer = null,
        IOperator<Evaluation, TCandidate>? evaluator = null,
        IOperator<Refinement, TCandidate>? refiner = null,
        IOperator<Interception<PopulationState<TCandidate>>, TCandidate>? interceptor = null,
        int populationSize = NSGA2Defaults.PopulationSize,
        int? maximumGenerations = NSGA2Defaults.MaximumGenerations,
        double mutationRate = NSGA2Defaults.MutationRate) =>
        new()
        {
            Creator = creator,
            Crossover = crossover,
            Mutator = mutator,
            Selector = selector ?? NSGA2Defaults.Selector<TCandidate>(),
            Replacer = replacer ?? NSGA2Defaults.Replacer<TCandidate>(),
            Evaluator = evaluator ?? NSGA2Defaults.Evaluator<TCandidate>(),
            Refiner = refiner,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            MaximumGenerations = maximumGenerations,
            MutationRate = mutationRate
        };

}

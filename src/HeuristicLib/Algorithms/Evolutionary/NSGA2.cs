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
    public override bool Fits(ExecutionSignature execution) => base.Fits(execution) && execution.Fits(Creator, Crossover, Mutator, Selector, Replacer, Evaluator, Refiner);

    /// <summary>
    /// Gets the generation limit, or <see langword="null"/> for no limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit completes before the first generation is produced.</remarks>
    public int? MaximumGenerations { get; init; } = NSGA2Defaults.MaximumGenerations;

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
            PopulationSize, MaximumGenerations, MutationRate);
    }

    private sealed class Executor<TSearchSpace, TProblem>(
        IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
        IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreationExecutor<TCandidate, TSearchSpace, TProblem> creator,
        ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> crossover,
        IMutationExecutor<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectionExecutor<TCandidate, TSearchSpace, TProblem> selector,
        IReplacementExecutor<TCandidate, TSearchSpace, TProblem> replacer,
        IRefinementExecutor<TCandidate, TSearchSpace, TProblem>? refiner,
        int populationSize,
        int? maximumGenerations,
        double mutationRate)
        : IterativeAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>(interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly WeightedRouter<IMutationExecutor<TCandidate, TSearchSpace, TProblem>?> mutationRouter = WeightedRouter.AtRate(mutator, mutationRate);

        protected override bool HasCompleted(int yieldedStateCount, PopulationState<TCandidate>? previousState, TProblem problem) =>
            maximumGenerations is not null && yieldedStateCount >= maximumGenerations.Value;

        protected override PopulationState<TCandidate> ExecuteStep(PopulationState<TCandidate>? previousState, TProblem problem, IRandomNumberGenerator random)
        {
            if (previousState is null)
            {
                var initialSolutions = creator.Create(populationSize, random, problem.SearchSpace, problem);
                if (refiner is not null)
                {
                    initialSolutions = refiner.Refine(initialSolutions, random, problem.SearchSpace, problem);
                }

                var initialPopulation = initialSolutions.ToEvaluated(evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem));
                return Population.From(initialPopulation).ToPopulationState();
            }

            var parents = selector.Select(previousState.Population.EvaluatedCandidates, problem.Objective, populationSize * 2, random, problem.SearchSpace, problem).ToParents(problem.Objective);
            var children = crossover.Cross(parents, random, problem.SearchSpace, problem);
            var mutants = mutationRouter.Route(children, random, (selected, batch) =>
                selected is null ? batch : selected.Mutate(batch, random, problem.SearchSpace, problem));
            if (refiner is not null)
            {
                mutants = refiner.Refine(mutants, random, problem.SearchSpace, problem);
            }

            var newPopulation = mutants.ToEvaluated(evaluator.Evaluate(mutants, random, problem.SearchSpace, problem));
            var nextPopulation = replacer.Replace(previousState.Population.EvaluatedCandidates, newPopulation, problem.Objective, populationSize, random, problem.SearchSpace, problem);

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

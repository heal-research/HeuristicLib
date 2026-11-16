using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public enum EvolutionStrategyType
{
    Comma,
    Plus
}

public record EvolutionStrategy<TCandidate>
    : IterativeAlgorithm<EvolutionStrategy<TCandidate>, TCandidate, PopulationState<TCandidate>>
{
    public int PopulationSize { get; init; } = EvolutionStrategyDefaults.PopulationSize;
    public int NumberOfChildren { get; init; } = EvolutionStrategyDefaults.NumberOfChildren;
    public EvolutionStrategyType Strategy { get; init; } = EvolutionStrategyDefaults.Strategy;
    public required IOperator<Creation, TCandidate> Creator { get; init; }
    public required IOperator<Mutation, TCandidate> Mutator { get; init; }
    public ScaleAdaptation? MutationStrengthAdaptation { get; init; }
    public IOperator<Crossover, TCandidate>? Crossover { get; init; }
    public IOperator<Evaluation, TCandidate> Evaluator { get; init; } = EvolutionStrategyDefaults.Evaluator<TCandidate>();
    public IOperator<Selection, TCandidate> Selector { get; init; } = EvolutionStrategyDefaults.Selector<TCandidate>();
    public IOperator<Refinement, TCandidate>? Refiner { get; init; }
    /// <summary>
    /// Gets the generation limit, or <see langword="null"/> for no limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit completes before the first generation is produced.</remarks>
    public int? MaximumGenerations { get; init; } = EvolutionStrategyDefaults.MaximumGenerations;

    public static Parameter<EvolutionStrategy<TCandidate>, int> PopulationSizeParameter { get; } =
        new(nameof(PopulationSize), static algorithm => algorithm.PopulationSize, static (algorithm, value) => algorithm with { PopulationSize = value });

    public static Parameter<EvolutionStrategy<TCandidate>, int> NumberOfChildrenParameter { get; } =
        new(nameof(NumberOfChildren), static algorithm => algorithm.NumberOfChildren, static (algorithm, value) => algorithm with { NumberOfChildren = value });

    public static Parameter<EvolutionStrategy<TCandidate>, EvolutionStrategyType> StrategyParameter { get; } =
        new(nameof(Strategy), static algorithm => algorithm.Strategy, static (algorithm, value) => algorithm with { Strategy = value });

    public static Parameter<EvolutionStrategy<TCandidate>, IOperator<Creation, TCandidate>> CreatorParameter { get; } =
        new(nameof(Creator), static algorithm => algorithm.Creator, static (algorithm, value) => algorithm with { Creator = value });

    public static Parameter<EvolutionStrategy<TCandidate>, IOperator<Mutation, TCandidate>> MutatorParameter { get; } =
        new(nameof(Mutator), static algorithm => algorithm.Mutator, static (algorithm, value) => algorithm with { Mutator = value });

    public static Parameter<EvolutionStrategy<TCandidate>, ScaleAdaptation?> MutationStrengthAdaptationParameter { get; } =
        new(nameof(MutationStrengthAdaptation), static algorithm => algorithm.MutationStrengthAdaptation, static (algorithm, value) => algorithm with { MutationStrengthAdaptation = value });

    public static Parameter<EvolutionStrategy<TCandidate>, IOperator<Crossover, TCandidate>?> CrossoverParameter { get; } =
        new(nameof(Crossover), static algorithm => algorithm.Crossover, static (algorithm, value) => algorithm with { Crossover = value });

    public static Parameter<EvolutionStrategy<TCandidate>, IOperator<Evaluation, TCandidate>> EvaluatorParameter { get; } =
        new(nameof(Evaluator), static algorithm => algorithm.Evaluator, static (algorithm, value) => algorithm with { Evaluator = value });

    public static Parameter<EvolutionStrategy<TCandidate>, IOperator<Selection, TCandidate>> SelectorParameter { get; } =
        new(nameof(Selector), static algorithm => algorithm.Selector, static (algorithm, value) => algorithm with { Selector = value });

    public static Parameter<EvolutionStrategy<TCandidate>, IOperator<Refinement, TCandidate>?> RefinerParameter { get; } =
        new(nameof(Refiner), static algorithm => algorithm.Refiner, static (algorithm, value) => algorithm with { Refiner = value });

    public static Parameter<EvolutionStrategy<TCandidate>, int?> MaximumGenerationsParameter { get; } =
        new(nameof(MaximumGenerations), static algorithm => algorithm.MaximumGenerations, static (algorithm, value) => algorithm with { MaximumGenerations = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [PopulationSizeParameter, NumberOfChildrenParameter, StrategyParameter, CreatorParameter, MutatorParameter, MutationStrengthAdaptationParameter, CrossoverParameter, EvaluatorParameter, SelectorParameter, RefinerParameter, MaximumGenerationsParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IterativeAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> scope)
    {
        var interceptor = scope.ResolveOptional(Interceptor);
        var evaluator = scope.Resolve(Evaluator);
        var creator = scope.Resolve(Creator);
        var mutator = scope.Resolve(Mutator);
        var adaptation = scope.ResolveOptional(MutationStrengthAdaptation);
        var selector = scope.Resolve(Selector);
        var crossover = scope.ResolveOptional(Crossover);
        var refiner = scope.ResolveOptional(Refiner);
        return new Executor<TSearchSpace, TProblem>(interceptor, evaluator, creator, mutator, adaptation, selector, crossover, refiner,
            PopulationSize, NumberOfChildren, Strategy, MaximumGenerations, scope.Problem);
    }

    private sealed class Executor<TSearchSpace, TProblem>(
        IInterceptionExecutor<PopulationState<TCandidate>>? interceptor,
        IEvaluationExecutor<TCandidate> evaluator,
        ICreationExecutor<TCandidate> creator,
        IMutationExecutor<TCandidate> mutator,
        AdaptedScale? adaptation,
        ISelectionExecutor<TCandidate> selector,
        ICrossoverExecutor<TCandidate>? crossover,
        IRefinementExecutor<TCandidate>? refiner,
        int populationSize,
        int numberOfChildren,
        EvolutionStrategyType strategy,
        int? maximumGenerations, TProblem problem)
        : IterativeAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>(interceptor, problem)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        protected override bool HasCompleted(int yieldedStateCount, PopulationState<TCandidate>? previousState) =>
            yieldedStateCount >= maximumGenerations;

        protected override PopulationState<TCandidate> ExecuteStep(PopulationState<TCandidate>? previousState, IRandomNumberGenerator random)
        {
            if (previousState is null)
            {
                var initialPopulation = creator.Create(populationSize, random);
                if (refiner is not null)
                {
                    initialPopulation = refiner.Refine(initialPopulation, random);
                }

                var objectiveVectors = evaluator.Evaluate(initialPopulation, random);
                return Population.From(initialPopulation.ToEvaluated(objectiveVectors)).ToPopulationState();
            }

            IReadOnlyList<TCandidate> parents;
            IReadOnlyList<ObjectiveVector> parentQualities;

            if (crossover is null)
            {
                var parentSolutions = selector.Select(previousState.Population.EvaluatedCandidates, Problem.Objective, numberOfChildren, random);
                parents = parentSolutions.Select(x => x.Candidate).ToArray();
                parentQualities = parentSolutions.Select(x => x.ObjectiveVector).ToArray();
            }
            else
            {
                var parentSolutions = selector.Select(previousState.Population.EvaluatedCandidates, Problem.Objective, numberOfChildren * 2, random);
                parents = crossover.Cross(parentSolutions.ToParents(Problem.Objective), random);
                parentQualities = parentSolutions.Where((_, i) => i % 2 == 0).Select(x => x.ObjectiveVector).ToArray();
            }

            var children = mutator.Mutate(parents, random);
            if (refiner is not null)
            {
                children = refiner.Refine(children, random);
            }

            var evaluatedChildren = children.ToEvaluated(evaluator.Evaluate(children, random));

            if (adaptation is not null)
            {
                // The rate is over the parent/child pairs actually compared, which is the number of children rather
                // than the population size; the two differ whenever the strategy is configured with more or fewer
                // children than parents.
                var comparisons = parentQualities.Zip(evaluatedChildren).ToArray();
                var successes = comparisons.Count(
                    pair => pair.Second.ObjectiveVector.CompareTo(pair.First, Problem.Objective) == DominanceRelation.Dominates);
                var successRate = comparisons.Length == 0 ? 0.0 : successes / (double)comparisons.Length;
                adaptation.Factor *= successRate switch
                {
                    > 0.2 => 1.5,
                    < 0.2 => 1 / 1.5,
                    _ => 1
                };
            }

            var population = Population.From(evaluatedChildren);
            var newPopulation = strategy switch
            {
                EvolutionStrategyType.Comma => ElitismReplacer.Replace(previousState.Population.EvaluatedCandidates, population.EvaluatedCandidates, Problem.Objective, numberOfChildren, 0),
                EvolutionStrategyType.Plus => PlusSelectionReplacer.Replace(previousState.Population.EvaluatedCandidates, population.EvaluatedCandidates, Problem.Objective, numberOfChildren),
                _ => throw new InvalidOperationException($"Unknown strategy {strategy}")
            };

            return Population.From(newPopulation).ToPopulationState();
        }
    }
}

public static class EvolutionStrategy
{
    /// <summary>
    /// Creates an evolution strategy for a problem, asking the problem first and its search space second for every
    /// required operator the caller does not supply.
    /// </summary>
    /// <remarks>
    /// The creator and mutator are selected independently. An explicit argument wins, followed by a problem
    /// recommendation and then a search space recommendation. The selector and evaluator come from
    /// <see cref="EvolutionStrategyDefaults"/> when omitted. Crossover and the other optional operators are not
    /// populated from recommendations.
    /// </remarks>
    /// <exception cref="InvalidOperationException">No value or recommendation is available for one or more required operators.</exception>
    public static EvolutionStrategy<TCandidate> For<TProblem, TCandidate, TSearchSpace>(
        Problem<TProblem, TCandidate, TSearchSpace> problem,
        IOperator<Creation, TCandidate>? creator = null,
        IOperator<Mutation, TCandidate>? mutator = null,
        IOperator<Crossover, TCandidate>? crossover = null,
        IOperator<Selection, TCandidate>? selector = null,
        IOperator<Evaluation, TCandidate>? evaluator = null,
        IOperator<Refinement, TCandidate>? refiner = null,
        IOperator<Interception<PopulationState<TCandidate>>, TCandidate>? interceptor = null,
        int populationSize = EvolutionStrategyDefaults.PopulationSize,
        int numberOfChildren = EvolutionStrategyDefaults.NumberOfChildren,
        EvolutionStrategyType strategy = EvolutionStrategyDefaults.Strategy,
        int? maximumGenerations = EvolutionStrategyDefaults.MaximumGenerations)
        where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
        where TSearchSpace : class, ISearchSpace<TCandidate>
    {
        var searchSpace = problem.SearchSpace;
        var recommendations = new OperatorRecommendationResolution(problem, searchSpace);
        creator = recommendations.GetOrRecommend(nameof(creator), creator);
        mutator = recommendations.GetOrRecommend(nameof(mutator), mutator);
        recommendations.ThrowIfIncomplete(nameof(EvolutionStrategy));

        return new()
        {
            Creator = creator!,
            Mutator = mutator!,
            Crossover = crossover,
            Selector = selector ?? EvolutionStrategyDefaults.Selector<TCandidate>(),
            Evaluator = evaluator ?? EvolutionStrategyDefaults.Evaluator<TCandidate>(),
            Refiner = refiner,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            NumberOfChildren = numberOfChildren,
            Strategy = strategy,
            MaximumGenerations = maximumGenerations
        };
    }

    /// <summary>
    /// Creates an evolution strategy from a search space's creator and mutator recommendations, with no problem
    /// instance.
    /// </summary>
    /// <remarks>
    /// An explicit creator or mutator wins over its search space recommendation. The selector and evaluator come
    /// from <see cref="EvolutionStrategyDefaults"/> when omitted. Crossover and the other optional operators are not
    /// populated from recommendations.
    /// </remarks>
    /// <exception cref="InvalidOperationException">No value or recommendation is available for one or more required operators.</exception>
    public static EvolutionStrategy<TCandidate> For<TCandidate>(
        ISearchSpace<TCandidate> searchSpace,
        IOperator<Creation, TCandidate>? creator = null,
        IOperator<Mutation, TCandidate>? mutator = null,
        IOperator<Crossover, TCandidate>? crossover = null,
        IOperator<Selection, TCandidate>? selector = null,
        IOperator<Evaluation, TCandidate>? evaluator = null,
        IOperator<Refinement, TCandidate>? refiner = null,
        IOperator<Interception<PopulationState<TCandidate>>, TCandidate>? interceptor = null,
        int populationSize = EvolutionStrategyDefaults.PopulationSize,
        int numberOfChildren = EvolutionStrategyDefaults.NumberOfChildren,
        EvolutionStrategyType strategy = EvolutionStrategyDefaults.Strategy,
        int? maximumGenerations = EvolutionStrategyDefaults.MaximumGenerations)
    {
        var recommendations = new OperatorRecommendationResolution(problem: null, searchSpace);
        creator = recommendations.GetOrRecommend(nameof(creator), creator);
        mutator = recommendations.GetOrRecommend(nameof(mutator), mutator);
        recommendations.ThrowIfIncomplete(nameof(EvolutionStrategy));

        return new()
        {
            Creator = creator!,
            Mutator = mutator!,
            Crossover = crossover,
            Selector = selector ?? EvolutionStrategyDefaults.Selector<TCandidate>(),
            Evaluator = evaluator ?? EvolutionStrategyDefaults.Evaluator<TCandidate>(),
            Refiner = refiner,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            NumberOfChildren = numberOfChildren,
            Strategy = strategy,
            MaximumGenerations = maximumGenerations
        };
    }

    /// <summary>
    /// Creates an evolution strategy from the operators it requires, inferring the candidate type from them. Every
    /// remaining member is optional and falls back to <see cref="EvolutionStrategyDefaults"/>.
    /// </summary>
    public static EvolutionStrategy<TCandidate> Create<TCandidate>(
        IOperator<Creation, TCandidate> creator,
        IOperator<Mutation, TCandidate> mutator,
        IOperator<Crossover, TCandidate>? crossover = null,
        IOperator<Selection, TCandidate>? selector = null,
        IOperator<Evaluation, TCandidate>? evaluator = null,
        IOperator<Refinement, TCandidate>? refiner = null,
        IOperator<Interception<PopulationState<TCandidate>>, TCandidate>? interceptor = null,
        int populationSize = EvolutionStrategyDefaults.PopulationSize,
        int numberOfChildren = EvolutionStrategyDefaults.NumberOfChildren,
        EvolutionStrategyType strategy = EvolutionStrategyDefaults.Strategy,
        int? maximumGenerations = EvolutionStrategyDefaults.MaximumGenerations) =>
        new()
        {
            Creator = creator,
            Mutator = mutator,
            Crossover = crossover,
            Selector = selector ?? EvolutionStrategyDefaults.Selector<TCandidate>(),
            Evaluator = evaluator ?? EvolutionStrategyDefaults.Evaluator<TCandidate>(),
            Refiner = refiner,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            NumberOfChildren = numberOfChildren,
            Strategy = strategy,
            MaximumGenerations = maximumGenerations
        };
}

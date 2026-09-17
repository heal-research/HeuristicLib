using HEAL.HeuristicLib.Encodings.RealVectors;
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
    public required ICreator<TCandidate> Creator { get; init; }
    public required IMutator<TCandidate> Mutator { get; init; }
    public ICrossover<TCandidate>? Crossover { get; init; }
    public IEvaluator<TCandidate> Evaluator { get; init; } = EvolutionStrategyDefaults.Evaluator<TCandidate>();
    public ISelector<TCandidate> Selector { get; init; } = EvolutionStrategyDefaults.Selector<TCandidate>();
    public IRefiner<TCandidate>? Refiner { get; init; }
    public override bool Fits(ExecutionSignature execution) => base.Fits(execution) && execution.Fits(Creator, Mutator, Crossover, Evaluator, Selector, Refiner);

    /// <summary>
    /// Gets the generation limit, or <see langword="null"/> for no limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit completes before the first generation is produced.</remarks>
    public int? MaximumGenerations { get; init; } = EvolutionStrategyDefaults.MaximumGenerations;

    protected override IterativeAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, PopulationState<TCandidate>> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry, IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, PopulationState<TCandidate>>? resolvedInterceptor)
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return new Instance<TRunSearchSpace, TRunProblem>(resolvedInterceptor, resolver.Resolve(Evaluator), resolver.Resolve(Creator), resolver.Resolve(Mutator), resolver.Resolve(Selector),
            resolver.ResolveOptional(Crossover), resolver.ResolveOptional(Refiner), PopulationSize, NumberOfChildren, Strategy, MaximumGenerations);
    }

    private sealed class Instance<TSearchSpace, TProblem>(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorInstance<TCandidate, TSearchSpace, TProblem> selector,
        ICrossoverInstance<TCandidate, TSearchSpace, TProblem>? crossover,
        IRefinerInstance<TCandidate, TSearchSpace, TProblem>? refiner,
        int populationSize,
        int numberOfChildren,
        EvolutionStrategyType strategy,
        int? maximumGenerations)
        : IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>(interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        protected override bool HasCompleted(int yieldedStateCount, PopulationState<TCandidate>? previousState, TProblem problem) =>
            yieldedStateCount >= maximumGenerations;

        protected override PopulationState<TCandidate> ExecuteStep(PopulationState<TCandidate>? previousState, TProblem problem, IRandomNumberGenerator random)
        {
            if (previousState is null)
            {
                var initialPopulation = creator.Create(populationSize, random, problem.SearchSpace, problem);
                if (refiner is not null)
                {
                    initialPopulation = refiner.Refine(initialPopulation, random, problem.SearchSpace, problem);
                }

                var objectiveVectors = evaluator.Evaluate(initialPopulation, random, problem.SearchSpace, problem);
                return Population.From(initialPopulation.ToEvaluated(objectiveVectors)).ToPopulationState();
            }

            IReadOnlyList<TCandidate> parents;
            IReadOnlyList<ObjectiveVector> parentQualities;

            if (crossover is null)
            {
                var parentSolutions = selector.Select(previousState.Population.EvaluatedCandidates, problem.Objective, numberOfChildren, random, problem.SearchSpace, problem);
                parents = parentSolutions.Select(x => x.Candidate).ToArray();
                parentQualities = parentSolutions.Select(x => x.ObjectiveVector).ToArray();
            }
            else
            {
                var parentSolutions = selector.Select(previousState.Population.EvaluatedCandidates, problem.Objective, numberOfChildren * 2, random, problem.SearchSpace, problem);
                parents = crossover.Cross(parentSolutions.ToParents(problem.Objective), random, problem.SearchSpace, problem);
                parentQualities = parentSolutions.Where((_, i) => i % 2 == 0).Select(x => x.ObjectiveVector).ToArray();
            }

            var children = mutator.Mutate(parents, random, problem.SearchSpace, problem);
            if (refiner is not null)
            {
                children = refiner.Refine(children, random, problem.SearchSpace, problem);
            }

            var evaluatedChildren = children.ToEvaluated(evaluator.Evaluate(children, random, problem.SearchSpace, problem));

            if (mutator is IAdaptableMutationStrengthInstance<TCandidate, TSearchSpace, TProblem> adaptableMutator)
            {
                // The rate is over the parent/child pairs actually compared, which is the number of children rather
                // than the population size; the two differ whenever the strategy is configured with more or fewer
                // children than parents.
                var comparisons = parentQualities.Zip(evaluatedChildren).ToArray();
                var successes = comparisons.Count(
                    pair => pair.Second.ObjectiveVector.CompareTo(pair.First, problem.Objective) == DominanceRelation.Dominates);
                var successRate = comparisons.Length == 0 ? 0.0 : successes / (double)comparisons.Length;
                adaptableMutator.CurrentMutationStrength *= successRate switch
                {
                    > 0.2 => 1.5,
                    < 0.2 => 1 / 1.5,
                    _ => 1
                };
            }

            var population = Population.From(evaluatedChildren);
            var newPopulation = strategy switch
            {
                EvolutionStrategyType.Comma => ElitismReplacer.Replace(previousState.Population.EvaluatedCandidates, population.EvaluatedCandidates, problem.Objective, numberOfChildren, 0),
                EvolutionStrategyType.Plus => PlusSelectionReplacer.Replace(previousState.Population.EvaluatedCandidates, population.EvaluatedCandidates, problem.Objective, numberOfChildren),
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
        ICreator<TCandidate>? creator = null,
        IMutator<TCandidate>? mutator = null,
        ICrossover<TCandidate>? crossover = null,
        ISelector<TCandidate>? selector = null,
        IEvaluator<TCandidate>? evaluator = null,
        IRefiner<TCandidate>? refiner = null,
        IInterceptor<TCandidate>? interceptor = null,
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
        ICreator<TCandidate>? creator = null,
        IMutator<TCandidate>? mutator = null,
        ICrossover<TCandidate>? crossover = null,
        ISelector<TCandidate>? selector = null,
        IEvaluator<TCandidate>? evaluator = null,
        IRefiner<TCandidate>? refiner = null,
        IInterceptor<TCandidate>? interceptor = null,
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
        ICreator<TCandidate> creator,
        IMutator<TCandidate> mutator,
        ICrossover<TCandidate>? crossover = null,
        ISelector<TCandidate>? selector = null,
        IEvaluator<TCandidate>? evaluator = null,
        IRefiner<TCandidate>? refiner = null,
        IInterceptor<TCandidate>? interceptor = null,
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

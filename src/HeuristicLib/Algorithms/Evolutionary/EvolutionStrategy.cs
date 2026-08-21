using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public enum EvolutionStrategyType
{
    Comma,
    Plus
}

public record EvolutionStrategy<TCandidate, TSearchSpace, TProblem>
    : IterativeAlgorithm<EvolutionStrategy<TCandidate, TSearchSpace, TProblem>, TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public int PopulationSize { get; init; } = EvolutionStrategyDefaults.PopulationSize;
    public int NumberOfChildren { get; init; } = EvolutionStrategyDefaults.NumberOfChildren;
    public EvolutionStrategyType Strategy { get; init; } = EvolutionStrategyDefaults.Strategy;
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
    public ICrossover<TCandidate, TSearchSpace, TProblem>? Crossover { get; init; }
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; } = EvolutionStrategyDefaults.Evaluator<TCandidate, TSearchSpace, TProblem>();
    public ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; init; } = EvolutionStrategyDefaults.Selector<TCandidate, TSearchSpace, TProblem>();
    public IRefiner<TCandidate, TSearchSpace, TProblem>? Refiner { get; init; }

    /// <summary>
    /// Gets the generation limit, or <see langword="null"/> for no limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit completes before the first generation is produced.</remarks>
    public int? MaximumGenerations { get; init; }

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? resolvedInterceptor)
    {
        var mutator = instanceRegistry.Resolve(Mutator);
        return new Instance(resolvedInterceptor, instanceRegistry.Resolve(Evaluator), instanceRegistry.Resolve(Creator), mutator, instanceRegistry.Resolve(Selector), instanceRegistry.ResolveOptional(Crossover), instanceRegistry.ResolveOptional(Refiner), PopulationSize, NumberOfChildren, Strategy, MaximumGenerations);
    }

    private sealed class Instance(
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

            if (mutator is IVariableStrengthMutatorInstance<TCandidate, TSearchSpace, TProblem> variableStrengthMutator)
            {
                var successes = parentQualities.Zip(evaluatedChildren).Count(
                    pair => pair.Second.ObjectiveVector.CompareTo(pair.First, problem.Objective) == DominanceRelation.Dominates);
                var successRate = successes / (double)populationSize;
                variableStrengthMutator.CurrentMutationStrength *= successRate switch
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
    /// Creates an evolution strategy for a problem that states its own operator preferences, asking the problem first
    /// and falling back to the search space's encoding defaults for the required creator and mutator.
    /// </summary>
    public static EvolutionStrategy<TCandidate, TSearchSpace, TProblem> For<TProblem, TCandidate, TSearchSpace>(
        IProblemDefaults<TProblem, TCandidate, TSearchSpace> problem,
        ICreator<TCandidate, TSearchSpace, TProblem>? creator = null,
        IMutator<TCandidate, TSearchSpace, TProblem>? mutator = null,
        ICrossover<TCandidate, TSearchSpace, TProblem>? crossover = null,
        ISelector<TCandidate, TSearchSpace, TProblem>? selector = null,
        IEvaluator<TCandidate, TSearchSpace, TProblem>? evaluator = null,
        IRefiner<TCandidate, TSearchSpace, TProblem>? refiner = null,
        IInterceptor<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor = null,
        int populationSize = EvolutionStrategyDefaults.PopulationSize,
        int numberOfChildren = EvolutionStrategyDefaults.NumberOfChildren,
        EvolutionStrategyType strategy = EvolutionStrategyDefaults.Strategy,
        int? maximumGenerations = null)
        where TProblem : class,
                         IProblemDefaultCreator<TProblem, TCandidate, TSearchSpace>,
                         IProblemDefaultMutator<TProblem, TCandidate, TSearchSpace>
        where TSearchSpace : class, ISearchSpace<TCandidate>,
                             IEncodingDefaultCreator<TCandidate, TSearchSpace>,
                             IEncodingDefaultMutator<TCandidate, TSearchSpace>
    {
        var searchSpace = problem.SearchSpace;
        var self = problem as TProblem;

        return new()
        {
            Creator = creator ?? (self is null ? null : TProblem.CreateDefaultCreator(self)) ?? TSearchSpace.CreateDefaultCreator(searchSpace),
            Mutator = mutator ?? (self is null ? null : TProblem.CreateDefaultMutator(self)) ?? TSearchSpace.CreateDefaultMutator(searchSpace),
            Crossover = crossover,
            Selector = selector ?? EvolutionStrategyDefaults.Selector<TCandidate, TSearchSpace, TProblem>(),
            Evaluator = evaluator ?? EvolutionStrategyDefaults.Evaluator<TCandidate, TSearchSpace, TProblem>(),
            Refiner = refiner,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            NumberOfChildren = numberOfChildren,
            Strategy = strategy,
            MaximumGenerations = maximumGenerations
        };
    }

    /// <summary>
    /// Creates an evolution strategy from a search space's required creator and mutator defaults, with no problem
    /// instance.
    /// </summary>
    public static EvolutionStrategy<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> For<TCandidate, TSearchSpace>(
        IEncodingDefaults<TCandidate, TSearchSpace> searchSpace,
        ICreator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? creator = null,
        IMutator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? mutator = null,
        ICrossover<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? crossover = null,
        ISelector<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? selector = null,
        IEvaluator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? evaluator = null,
        IRefiner<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? refiner = null,
        IInterceptor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, PopulationState<TCandidate>>? interceptor = null,
        int populationSize = EvolutionStrategyDefaults.PopulationSize,
        int numberOfChildren = EvolutionStrategyDefaults.NumberOfChildren,
        EvolutionStrategyType strategy = EvolutionStrategyDefaults.Strategy,
        int? maximumGenerations = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>,
                             IEncodingDefaultCreator<TCandidate, TSearchSpace>,
                             IEncodingDefaultMutator<TCandidate, TSearchSpace>
    {
        var typedSearchSpace = (TSearchSpace)searchSpace;

        return new()
        {
            Creator = creator ?? TSearchSpace.CreateDefaultCreator(typedSearchSpace),
            Mutator = mutator ?? TSearchSpace.CreateDefaultMutator(typedSearchSpace),
            Crossover = crossover,
            Selector = selector ?? EvolutionStrategyDefaults.Selector<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>(),
            Evaluator = evaluator ?? EvolutionStrategyDefaults.Evaluator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>(),
            Refiner = refiner,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            NumberOfChildren = numberOfChildren,
            Strategy = strategy,
            MaximumGenerations = maximumGenerations
        };
    }

    /// <summary>
    /// Creates an evolution strategy from the operators it requires, inferring the candidate, search space and problem
    /// types from them. Every remaining member is optional and falls back to <see cref="EvolutionStrategyDefaults"/>.
    /// </summary>
    public static EvolutionStrategy<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(
        ICreator<TCandidate, TSearchSpace, TProblem> creator,
        IMutator<TCandidate, TSearchSpace, TProblem> mutator,
        ICrossover<TCandidate, TSearchSpace, TProblem>? crossover = null,
        ISelector<TCandidate, TSearchSpace, TProblem>? selector = null,
        IEvaluator<TCandidate, TSearchSpace, TProblem>? evaluator = null,
        IRefiner<TCandidate, TSearchSpace, TProblem>? refiner = null,
        IInterceptor<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor = null,
        int populationSize = EvolutionStrategyDefaults.PopulationSize,
        int numberOfChildren = EvolutionStrategyDefaults.NumberOfChildren,
        EvolutionStrategyType strategy = EvolutionStrategyDefaults.Strategy,
        int? maximumGenerations = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new()
        {
            Creator = creator,
            Mutator = mutator,
            Crossover = crossover,
            Selector = selector ?? EvolutionStrategyDefaults.Selector<TCandidate, TSearchSpace, TProblem>(),
            Evaluator = evaluator ?? EvolutionStrategyDefaults.Evaluator<TCandidate, TSearchSpace, TProblem>(),
            Refiner = refiner,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            NumberOfChildren = numberOfChildren,
            Strategy = strategy,
            MaximumGenerations = maximumGenerations
        };

}

using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

#pragma warning disable S101
public record NSGA2<TCandidate, TSearchSpace, TProblem>
#pragma warning restore S101
    : IterativeAlgorithm<NSGA2<TCandidate, TSearchSpace, TProblem>, TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public int PopulationSize { get; init; } = NSGA2Defaults.PopulationSize;
    public required ICreator<TCandidate> Creator { get; init; }
    public required ICrossover<TCandidate> Crossover { get; init; }
    public required IMutator<TCandidate> Mutator { get; init; }
    public ISelector<TCandidate> Selector { get; init; } = NSGA2Defaults.Selector<TCandidate, TSearchSpace, TProblem>();
    public IReplacer<TCandidate> Replacer { get; init; } = NSGA2Defaults.Replacer<TCandidate, TSearchSpace, TProblem>();
    public IEvaluator<TCandidate> Evaluator { get; init; } = NSGA2Defaults.Evaluator<TCandidate, TSearchSpace, TProblem>();

    /// <summary>
    /// Gets the probability that an offspring is mutated. The expected value is in <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The rate is applied as a threshold against a random value in <c>[0, 1)</c>. A value at most zero, negative
    /// infinity and <c>NaN</c> never mutate; a value at least one and positive infinity always mutate.
    /// </remarks>
    public double MutationRate { get; init; } = NSGA2Defaults.MutationRate;
    public IRefiner<TCandidate>? Refiner { get; init; }

    /// <summary>
    /// Gets the generation limit, or <see langword="null"/> for no limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit completes before the first generation is produced.</remarks>
    public int? MaximumGenerations { get; init; }

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? resolvedInterceptor)
    {
        var resolver = instanceRegistry.For<TCandidate, TSearchSpace, TProblem>();
        return new Instance(resolvedInterceptor, resolver.Resolve(Evaluator), resolver.Resolve(Creator), resolver.Resolve(Crossover),
            resolver.Resolve(MutationRate >= 1.0 ? Mutator : Mutator.WithRate(MutationRate)), resolver.Resolve(Selector),
            resolver.Resolve(Replacer), resolver.ResolveOptional(Refiner), PopulationSize, MaximumGenerations);
    }

    private sealed class Instance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        ICrossoverInstance<TCandidate, TSearchSpace, TProblem> crossover,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorInstance<TCandidate, TSearchSpace, TProblem> selector,
        IReplacerInstance<TCandidate, TSearchSpace, TProblem> replacer,
        IRefinerInstance<TCandidate, TSearchSpace, TProblem>? refiner,
        int populationSize,
        int? maximumGenerations)
        : IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>(interceptor)
    {
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
            var mutants = mutator.Mutate(children, random, problem.SearchSpace, problem);
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
    /// Creates an NSGA-II for a problem that states its own operator preferences, asking the problem first and falling
    /// back to the search space's encoding defaults for every required role.
    /// </summary>
    public static NSGA2<TCandidate, TSearchSpace, TProblem> For<TProblem, TCandidate, TSearchSpace>(
        Problem<TProblem, TCandidate, TSearchSpace> problem,
        ICreator<TCandidate>? creator = null,
        ICrossover<TCandidate>? crossover = null,
        IMutator<TCandidate>? mutator = null,
        ISelector<TCandidate>? selector = null,
        IReplacer<TCandidate>? replacer = null,
        IEvaluator<TCandidate>? evaluator = null,
        IRefiner<TCandidate>? refiner = null,
        IInterceptor<TCandidate>? interceptor = null,
        int populationSize = NSGA2Defaults.PopulationSize,
        int? maximumGenerations = null,
        double mutationRate = NSGA2Defaults.MutationRate)
        where TProblem : Problem<TProblem, TCandidate, TSearchSpace>,
                         IProblemDefaultCreator<TProblem, TCandidate, TSearchSpace>,
                         IProblemDefaultCrossover<TProblem, TCandidate, TSearchSpace>,
                         IProblemDefaultMutator<TProblem, TCandidate, TSearchSpace>
        where TSearchSpace : class, ISearchSpace<TCandidate>,
                             IEncodingDefaultCreator<TCandidate, TSearchSpace>,
                             IEncodingDefaultCrossover<TCandidate, TSearchSpace>,
                             IEncodingDefaultMutator<TCandidate, TSearchSpace>
    {
        var searchSpace = problem.SearchSpace;
        var self = problem as TProblem;

        return new()
        {
            Creator = creator ?? (self is null ? null : TProblem.CreateDefaultCreator(self)) ?? TSearchSpace.CreateDefaultCreator(searchSpace),
            Crossover = crossover ?? (self is null ? null : TProblem.CreateDefaultCrossover(self)) ?? TSearchSpace.CreateDefaultCrossover(searchSpace),
            Mutator = mutator ?? (self is null ? null : TProblem.CreateDefaultMutator(self)) ?? TSearchSpace.CreateDefaultMutator(searchSpace),
            Selector = selector ?? NSGA2Defaults.Selector<TCandidate, TSearchSpace, TProblem>(),
            Replacer = replacer ?? NSGA2Defaults.Replacer<TCandidate, TSearchSpace, TProblem>(),
            Evaluator = evaluator ?? NSGA2Defaults.Evaluator<TCandidate, TSearchSpace, TProblem>(),
            Refiner = refiner,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            MaximumGenerations = maximumGenerations,
            MutationRate = mutationRate
        };
    }

    /// <summary>
    /// Creates an NSGA-II from a search space's encoding defaults alone, with no problem instance.
    /// </summary>
    public static NSGA2<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> For<TCandidate, TSearchSpace>(
        IEncodingDefaults<TCandidate, TSearchSpace> searchSpace,
        ICreator<TCandidate>? creator = null,
        ICrossover<TCandidate>? crossover = null,
        IMutator<TCandidate>? mutator = null,
        ISelector<TCandidate>? selector = null,
        IReplacer<TCandidate>? replacer = null,
        IEvaluator<TCandidate>? evaluator = null,
        IRefiner<TCandidate>? refiner = null,
        IInterceptor<TCandidate>? interceptor = null,
        int populationSize = NSGA2Defaults.PopulationSize,
        int? maximumGenerations = null,
        double mutationRate = NSGA2Defaults.MutationRate)
        where TSearchSpace : class, ISearchSpace<TCandidate>,
                             IEncodingDefaultCreator<TCandidate, TSearchSpace>,
                             IEncodingDefaultCrossover<TCandidate, TSearchSpace>,
                             IEncodingDefaultMutator<TCandidate, TSearchSpace>
    {
        var typedSearchSpace = (TSearchSpace)searchSpace;

        return new()
        {
            Creator = creator ?? TSearchSpace.CreateDefaultCreator(typedSearchSpace),
            Crossover = crossover ?? TSearchSpace.CreateDefaultCrossover(typedSearchSpace),
            Mutator = mutator ?? TSearchSpace.CreateDefaultMutator(typedSearchSpace),
            Selector = selector ?? NSGA2Defaults.Selector<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>(),
            Replacer = replacer ?? NSGA2Defaults.Replacer<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>(),
            Evaluator = evaluator ?? NSGA2Defaults.Evaluator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>(),
            Refiner = refiner,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            MaximumGenerations = maximumGenerations,
            MutationRate = mutationRate
        };
    }

    /// <summary>
    /// Creates an NSGA-II from the operators it requires, inferring the candidate, search space and problem types from
    /// them. Every remaining member is optional and falls back to <see cref="NSGA2Defaults"/>.
    /// </summary>
    public static NSGA2<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(
        ICreator<TCandidate> creator,
        ICrossover<TCandidate> crossover,
        IMutator<TCandidate> mutator,
        ISelector<TCandidate>? selector = null,
        IReplacer<TCandidate>? replacer = null,
        IEvaluator<TCandidate>? evaluator = null,
        IRefiner<TCandidate>? refiner = null,
        IInterceptor<TCandidate>? interceptor = null,
        int populationSize = NSGA2Defaults.PopulationSize,
        int? maximumGenerations = null,
        double mutationRate = NSGA2Defaults.MutationRate)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new()
        {
            Creator = creator,
            Crossover = crossover,
            Mutator = mutator,
            Selector = selector ?? NSGA2Defaults.Selector<TCandidate, TSearchSpace, TProblem>(),
            Replacer = replacer ?? NSGA2Defaults.Replacer<TCandidate, TSearchSpace, TProblem>(),
            Evaluator = evaluator ?? NSGA2Defaults.Evaluator<TCandidate, TSearchSpace, TProblem>(),
            Refiner = refiner,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            MaximumGenerations = maximumGenerations,
            MutationRate = mutationRate
        };

}

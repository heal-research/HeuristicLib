using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record GeneticAlgorithm<TCandidate, TSearchSpace, TProblem>
    : IterativeAlgorithm<GeneticAlgorithm<TCandidate, TSearchSpace, TProblem>, TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public int PopulationSize { get; init; } = GeneticAlgorithmDefaults.PopulationSize;
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem> Crossover { get; init; }
    public required IMutator<TCandidate> Mutator { get; init; }
    public ITerminator<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? Terminator { get; init; }
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; } = GeneticAlgorithmDefaults.Evaluator<TCandidate, TSearchSpace, TProblem>();
    public IRefiner<TCandidate, TSearchSpace, TProblem>? Refiner { get; init; }

    /// <summary>
    /// Gets the generation limit, or <see langword="null"/> for no limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit completes before the first generation is produced.</remarks>
    public int? MaximumGenerations { get; init; }

    public int Elites { get; init; } = GeneticAlgorithmDefaults.Elites;

    /// <summary>
    /// Gets the probability that an offspring is mutated. The expected value is in <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The rate is applied as a threshold against a random value in <c>[0, 1)</c>. A value at most zero, negative
    /// infinity and <c>NaN</c> never mutate; a value at least one and positive infinity always mutate.
    /// </remarks>
    public double MutationRate { get; init; } = GeneticAlgorithmDefaults.MutationRate;

    public ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; init; } = GeneticAlgorithmDefaults.Selector<TCandidate, TSearchSpace, TProblem>();

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? resolvedInterceptor)
    {
        var effectiveMutator = MutationRate >= 1.0 ? Mutator : Mutator.WithRate(MutationRate);
        return new Instance(resolvedInterceptor, instanceRegistry.Resolve(Evaluator), instanceRegistry.Resolve(Creator), instanceRegistry.Resolve(Crossover),
            instanceRegistry.Resolve<TCandidate, TSearchSpace, TProblem>(effectiveMutator), instanceRegistry.Resolve(Selector), instanceRegistry.ResolveOptional(Terminator),
            instanceRegistry.ResolveOptional(Refiner), PopulationSize, MaximumGenerations, Elites);
    }

    private sealed class Instance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        ICrossoverInstance<TCandidate, TSearchSpace, TProblem> crossover,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorInstance<TCandidate, TSearchSpace, TProblem> selector,
        ITerminatorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? terminator,
        IRefinerInstance<TCandidate, TSearchSpace, TProblem>? refiner,
        int populationSize,
        int? maximumGenerations,
        int elites)
        : IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>(interceptor)
    {
        protected override bool HasCompleted(int yieldedStateCount, PopulationState<TCandidate>? previousState, TProblem problem) =>
            maximumGenerations is not null && yieldedStateCount >= maximumGenerations.Value;

        protected override bool IsTerminalState(PopulationState<TCandidate> state, int yieldedStateCount, PopulationState<TCandidate>? previousState, TProblem problem) =>
            terminator?.IsTerminalState(state, problem.SearchSpace, problem) == true;

        protected override PopulationState<TCandidate> ExecuteStep(PopulationState<TCandidate>? previousState, TProblem problem, IRandomNumberGenerator random)
        {
            if (previousState is null)
            {
                var initialSolutions = creator.Create(populationSize, random, problem.SearchSpace, problem);
                if (refiner is not null)
                {
                    initialSolutions = refiner.Refine(initialSolutions, random, problem.SearchSpace, problem);
                }

                var initialObjectiveVectors = evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
                return Population.From(initialSolutions.ToEvaluated(initialObjectiveVectors)).ToPopulationState();
            }

            var oldPopulation = previousState.Population.EvaluatedCandidates;
            var parentCount = populationSize * 2;
            var parents = selector.Select(oldPopulation, problem.Objective, parentCount, random, problem.SearchSpace, problem).Select(x => x.Candidate).ToList();
            var offspring = crossover.Cross(parents.ToParentPairs(), random, problem.SearchSpace, problem);
            offspring = mutator.Mutate(offspring, random, problem.SearchSpace, problem);
            if (refiner is not null)
            {
                offspring = refiner.Refine(offspring, random, problem.SearchSpace, problem);
            }

            var offspringPopulation = offspring.ToEvaluated(evaluator.Evaluate(offspring, random, problem.SearchSpace, problem));
            var newPopulation = ElitismReplacer.Replace(oldPopulation, offspringPopulation, problem.Objective, populationSize, elites);
            return Population.From(newPopulation).ToPopulationState();
        }
    }
}

public record GeneticAlgorithm<TCandidate, TSearchSpace> : GeneticAlgorithm<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public record GeneticAlgorithm<TCandidate> : GeneticAlgorithm<TCandidate, ISearchSpace<TCandidate>>;

public static class GeneticAlgorithm
{
    /// <summary>
    /// Creates a genetic algorithm for a problem that states its own operator preferences, asking the problem first
    /// and falling back to the search space's encoding defaults for every role the problem declines.
    /// </summary>
    /// <remarks>
    /// The self type on <see cref="IProblemDefaults{TSelf, TCandidate, TSearchSpace}"/> is what lets the concrete
    /// problem type be inferred here, so the result is typed at that problem rather than at <see cref="IProblem{T, TS}"/>.
    /// <para>
    /// Every operator is optional and overrides whatever the defaults would have supplied for that role. Supplying one
    /// does not widen the inferred search space, because the anchor argument fixes it exactly.
    /// </para>
    /// </remarks>
    public static GeneticAlgorithm<TCandidate, TSearchSpace, TProblem> For<TProblem, TCandidate, TSearchSpace>(
        IProblemDefaults<TProblem, TCandidate, TSearchSpace> problem,
        ICreator<TCandidate, TSearchSpace, TProblem>? creator = null,
        ICrossover<TCandidate, TSearchSpace, TProblem>? crossover = null,
        IMutator<TCandidate>? mutator = null,
        ISelector<TCandidate, TSearchSpace, TProblem>? selector = null,
        IEvaluator<TCandidate, TSearchSpace, TProblem>? evaluator = null,
        IRefiner<TCandidate, TSearchSpace, TProblem>? refiner = null,
        ITerminator<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? terminator = null,
        IInterceptor<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor = null,
        int populationSize = GeneticAlgorithmDefaults.PopulationSize,
        int? maximumGenerations = null,
        double mutationRate = GeneticAlgorithmDefaults.MutationRate,
        int elites = GeneticAlgorithmDefaults.Elites)
        where TProblem : class,
                         IProblemDefaultCreator<TProblem, TCandidate, TSearchSpace>,
                         IProblemDefaultCrossover<TProblem, TCandidate, TSearchSpace>,
                         IProblemDefaultMutator<TProblem, TCandidate, TSearchSpace>
        where TSearchSpace : class, ISearchSpace<TCandidate>,
                             IEncodingDefaultCreator<TCandidate, TSearchSpace>,
                             IEncodingDefaultCrossover<TCandidate, TSearchSpace>,
                             IEncodingDefaultMutator<TCandidate, TSearchSpace>
    {
        var searchSpace = problem.SearchSpace;

        // The parameter is interface typed, so the concrete type has to be recovered. The self-type constraint makes
        // that correct only by convention, so a mis-declared problem degrades to the encoding defaults here rather
        // than throwing at run time.
        var self = problem as TProblem;

        return new()
        {
            Creator = creator ?? (self is null ? null : TProblem.CreateDefaultCreator(self)) ?? TSearchSpace.CreateDefaultCreator(searchSpace),
            Crossover = crossover ?? (self is null ? null : TProblem.CreateDefaultCrossover(self)) ?? TSearchSpace.CreateDefaultCrossover(searchSpace),
            Mutator = mutator ?? (self is null ? null : TProblem.CreateDefaultMutator(self)) ?? TSearchSpace.CreateDefaultMutator(searchSpace),
            Selector = selector ?? GeneticAlgorithmDefaults.Selector<TCandidate, TSearchSpace, TProblem>(),
            Evaluator = evaluator ?? GeneticAlgorithmDefaults.Evaluator<TCandidate, TSearchSpace, TProblem>(),
            Refiner = refiner,
            Terminator = terminator,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            MaximumGenerations = maximumGenerations,
            MutationRate = mutationRate,
            Elites = elites
        };
    }

    /// <summary>
    /// Creates a genetic algorithm from a search space's encoding defaults alone, with no problem instance.
    /// </summary>
    /// <remarks>
    /// The result runs against any problem over that search space, which is what makes it a reusable configuration.
    /// Pass the problem instead when that problem's own preferences should be consulted.
    /// <para>
    /// Every operator is optional and overrides whatever the defaults would have supplied for that role. Supplying one
    /// does not widen the inferred search space, because the anchor argument fixes it exactly.
    /// </para>
    /// </remarks>
    public static GeneticAlgorithm<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> For<TCandidate, TSearchSpace>(
        IEncodingDefaults<TCandidate, TSearchSpace> searchSpace,
        ICreator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? creator = null,
        ICrossover<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? crossover = null,
        IMutator<TCandidate>? mutator = null,
        ISelector<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? selector = null,
        IEvaluator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? evaluator = null,
        IRefiner<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? refiner = null,
        ITerminator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, PopulationState<TCandidate>>? terminator = null,
        IInterceptor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, PopulationState<TCandidate>>? interceptor = null,
        int populationSize = GeneticAlgorithmDefaults.PopulationSize,
        int? maximumGenerations = null,
        double mutationRate = GeneticAlgorithmDefaults.MutationRate,
        int elites = GeneticAlgorithmDefaults.Elites)
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
            Selector = selector ?? GeneticAlgorithmDefaults.Selector<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>(),
            Evaluator = evaluator ?? GeneticAlgorithmDefaults.Evaluator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>(),
            Refiner = refiner,
            Terminator = terminator,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            MaximumGenerations = maximumGenerations,
            MutationRate = mutationRate,
            Elites = elites
        };
    }

    /// <summary>
    /// Creates a genetic algorithm from the operators it requires, inferring the candidate, search space and problem
    /// types from them. Every remaining member is optional and falls back to <see cref="GeneticAlgorithmDefaults"/>.
    /// </summary>
    /// <remarks>
    /// An omitted operator contributes no bound, so leaving one out widens the inferred problem type and supplying a
    /// problem-bound one pins it. Use <see cref="For{TProblem, TCandidate, TSearchSpace}"/> instead when a problem or
    /// search space should supply the operators.
    /// </remarks>
    public static GeneticAlgorithm<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(
        ICreator<TCandidate, TSearchSpace, TProblem> creator,
        ICrossover<TCandidate, TSearchSpace, TProblem> crossover,
        IMutator<TCandidate> mutator,
        ISelector<TCandidate, TSearchSpace, TProblem>? selector = null,
        IEvaluator<TCandidate, TSearchSpace, TProblem>? evaluator = null,
        IRefiner<TCandidate, TSearchSpace, TProblem>? refiner = null,
        ITerminator<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? terminator = null,
        IInterceptor<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor = null,
        int populationSize = GeneticAlgorithmDefaults.PopulationSize,
        int? maximumGenerations = null,
        double mutationRate = GeneticAlgorithmDefaults.MutationRate,
        int elites = GeneticAlgorithmDefaults.Elites)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new()
        {
            Creator = creator,
            Crossover = crossover,
            Mutator = mutator,
            Selector = selector ?? GeneticAlgorithmDefaults.Selector<TCandidate, TSearchSpace, TProblem>(),
            Evaluator = evaluator ?? GeneticAlgorithmDefaults.Evaluator<TCandidate, TSearchSpace, TProblem>(),
            Refiner = refiner,
            Terminator = terminator,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            MaximumGenerations = maximumGenerations,
            MutationRate = mutationRate,
            Elites = elites
        };
}

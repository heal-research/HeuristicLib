using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record GeneticAlgorithm<TCandidate>
    : IterativeAlgorithm<GeneticAlgorithm<TCandidate>, TCandidate, PopulationState<TCandidate>>
{
    public int PopulationSize { get; init; } = GeneticAlgorithmDefaults.PopulationSize;
    public required ICreator<TCandidate> Creator { get; init; }
    public required ICrossover<TCandidate> Crossover { get; init; }
    public required IMutator<TCandidate> Mutator { get; init; }
    public ITerminator<TCandidate>? Terminator { get; init; }
    public IEvaluator<TCandidate> Evaluator { get; init; } = GeneticAlgorithmDefaults.Evaluator<TCandidate>();
    public IRefiner<TCandidate>? Refiner { get; init; }

    /// <summary>
    /// Gets the generation limit, or <see langword="null"/> for no limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit completes before the first generation is produced.</remarks>
    public int? MaximumGenerations { get; init; } = GeneticAlgorithmDefaults.MaximumGenerations;

    public int Elites { get; init; } = GeneticAlgorithmDefaults.Elites;

    /// <summary>
    /// Gets the probability that an offspring is mutated. The expected value is in <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The rate is applied as a threshold against a random value in <c>[0, 1)</c>. A value at most zero, negative
    /// infinity and <c>NaN</c> never mutate; a value at least one and positive infinity always mutate.
    /// </remarks>
    public double MutationRate { get; init; } = GeneticAlgorithmDefaults.MutationRate;

    public ISelector<TCandidate> Selector { get; init; } = GeneticAlgorithmDefaults.Selector<TCandidate>();

    protected override IterativeAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, PopulationState<TCandidate>> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry, IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, PopulationState<TCandidate>>? resolvedInterceptor)
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem, PopulationState<TCandidate>>();
        var effectiveMutator = MutationRate >= 1.0 ? Mutator : Mutator.AppliedAtRate(MutationRate);
        return new Instance<TRunSearchSpace, TRunProblem>(resolvedInterceptor, resolver.Resolve(Evaluator), resolver.Resolve(Creator), resolver.Resolve(Crossover),
            resolver.Resolve(effectiveMutator), resolver.Resolve(Selector), resolver.ResolveOptional(Terminator),
            resolver.ResolveOptional(Refiner), PopulationSize, MaximumGenerations, Elites);
    }

    private sealed class Instance<TSearchSpace, TProblem>(
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
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
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

public static class GeneticAlgorithm
{
    /// <summary>
    /// Creates a genetic algorithm for a problem that states its own operator preferences, asking the problem first
    /// and falling back to the search space's encoding defaults for every role the problem declines.
    /// </summary>
    /// <remarks>Every operator is optional and overrides whatever the defaults would have supplied for that role.</remarks>
    public static GeneticAlgorithm<TCandidate> For<TProblem, TCandidate, TSearchSpace>(
        Problem<TProblem, TCandidate, TSearchSpace> problem,
        ICreator<TCandidate>? creator = null,
        ICrossover<TCandidate>? crossover = null,
        IMutator<TCandidate>? mutator = null,
        ISelector<TCandidate>? selector = null,
        IEvaluator<TCandidate>? evaluator = null,
        IRefiner<TCandidate>? refiner = null,
        ITerminator<TCandidate>? terminator = null,
        IInterceptor<TCandidate>? interceptor = null,
        int populationSize = GeneticAlgorithmDefaults.PopulationSize,
        int? maximumGenerations = GeneticAlgorithmDefaults.MaximumGenerations,
        double mutationRate = GeneticAlgorithmDefaults.MutationRate,
        int elites = GeneticAlgorithmDefaults.Elites)
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

        // The parameter is interface typed, so the concrete type has to be recovered. The self-type constraint makes
        // that correct only by convention, so a mis-declared problem degrades to the encoding defaults here rather
        // than throwing at run time.
        var self = problem as TProblem;

        return new()
        {
            Creator = creator ?? (self is null ? null : TProblem.CreateDefaultCreator(self)) ?? TSearchSpace.CreateDefaultCreator(searchSpace),
            Crossover = crossover ?? (self is null ? null : TProblem.CreateDefaultCrossover(self)) ?? TSearchSpace.CreateDefaultCrossover(searchSpace),
            Mutator = mutator ?? (self is null ? null : TProblem.CreateDefaultMutator(self)) ?? TSearchSpace.CreateDefaultMutator(searchSpace),
            Selector = selector ?? GeneticAlgorithmDefaults.Selector<TCandidate>(),
            Evaluator = evaluator ?? GeneticAlgorithmDefaults.Evaluator<TCandidate>(),
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
    /// The result runs against any problem over that search space. Pass the problem instead when that problem's own
    /// preferences should be consulted.
    /// <para>
    /// Every operator is optional and overrides whatever the defaults would have supplied for that role.
    /// </para>
    /// </remarks>
    public static GeneticAlgorithm<TCandidate> For<TCandidate, TSearchSpace>(
        IEncodingDefaults<TCandidate, TSearchSpace> searchSpace,
        ICreator<TCandidate>? creator = null,
        ICrossover<TCandidate>? crossover = null,
        IMutator<TCandidate>? mutator = null,
        ISelector<TCandidate>? selector = null,
        IEvaluator<TCandidate>? evaluator = null,
        IRefiner<TCandidate>? refiner = null,
        ITerminator<TCandidate>? terminator = null,
        IInterceptor<TCandidate>? interceptor = null,
        int populationSize = GeneticAlgorithmDefaults.PopulationSize,
        int? maximumGenerations = GeneticAlgorithmDefaults.MaximumGenerations,
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
            Selector = selector ?? GeneticAlgorithmDefaults.Selector<TCandidate>(),
            Evaluator = evaluator ?? GeneticAlgorithmDefaults.Evaluator<TCandidate>(),
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
    /// Creates a genetic algorithm from the operators it requires, inferring the candidate type from them. Every
    /// remaining member is optional and falls back to <see cref="GeneticAlgorithmDefaults"/>.
    /// </summary>
    /// <remarks>
    /// Use <see cref="For{TProblem, TCandidate, TSearchSpace}"/> instead when a problem or search space should supply
    /// the operators.
    /// </remarks>
    public static GeneticAlgorithm<TCandidate> Create<TCandidate>(
        ICreator<TCandidate> creator,
        ICrossover<TCandidate> crossover,
        IMutator<TCandidate> mutator,
        ISelector<TCandidate>? selector = null,
        IEvaluator<TCandidate>? evaluator = null,
        IRefiner<TCandidate>? refiner = null,
        ITerminator<TCandidate>? terminator = null,
        IInterceptor<TCandidate>? interceptor = null,
        int populationSize = GeneticAlgorithmDefaults.PopulationSize,
        int? maximumGenerations = GeneticAlgorithmDefaults.MaximumGenerations,
        double mutationRate = GeneticAlgorithmDefaults.MutationRate,
        int elites = GeneticAlgorithmDefaults.Elites) =>
        new()
        {
            Creator = creator,
            Crossover = crossover,
            Mutator = mutator,
            Selector = selector ?? GeneticAlgorithmDefaults.Selector<TCandidate>(),
            Evaluator = evaluator ?? GeneticAlgorithmDefaults.Evaluator<TCandidate>(),
            Refiner = refiner,
            Terminator = terminator,
            Interceptor = interceptor,
            PopulationSize = populationSize,
            MaximumGenerations = maximumGenerations,
            MutationRate = mutationRate,
            Elites = elites
        };
}

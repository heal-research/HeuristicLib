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

public record GeneticAlgorithm<TCandidate, TSearchSpace, TProblem>
    : IterativeAlgorithm<GeneticAlgorithm<TCandidate, TSearchSpace, TProblem>, TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public required int PopulationSize { get; init; }
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem> Crossover { get; init; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
    public ITerminator<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? Terminator { get; init; }
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; } = new DirectEvaluator<TCandidate>();
    public int? MaximumGenerations
    {
        get;
        init => field = value is null or > 0
          ? value
          : throw new ArgumentOutOfRangeException(nameof(MaximumGenerations), "MaximumGenerations must be positive when set.");
    }

    public int Elites { get; init; } = 1;

    public double MutationRate
    {
        get;
        init => field = value is >= 0.0 and <= 1.0 ? value : throw new ArgumentOutOfRangeException(nameof(MutationRate), "MutationRate must be in [0, 1].");
    } = 0.1;

    public required ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; init; }

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> CreateIterativeAlgorithmInstance(
        ExecutionInstanceRegistry registry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? resolvedInterceptor)
    {
        var effectiveMutator = MutationRate >= 1.0 ? Mutator : Mutator.WithRate(MutationRate);
        return new Instance(resolvedInterceptor, registry.Resolve(Evaluator), registry.Resolve(Creator), registry.Resolve(Crossover),
            registry.Resolve(effectiveMutator), registry.Resolve(Selector), Terminator is null ? null : registry.Resolve(Terminator), PopulationSize, MaximumGenerations, Elites);
    }

    private sealed class Instance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        ICrossoverInstance<TCandidate, TSearchSpace, TProblem> crossover,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator,
        ISelectorInstance<TCandidate, TSearchSpace, TProblem> selector,
        ITerminatorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? terminator,
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
                var initialFitnesses = evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
                return new PopulationState<TCandidate> { Population = Population.From(initialSolutions, initialFitnesses) };
            }

            var oldPopulation = previousState.Population.EvaluatedCandidates;
            var offspringSize = populationSize * 2;
            var parents = selector.Select(oldPopulation, problem.Objective, offspringSize, random, problem.SearchSpace, problem).Select(x => x.Candidate).ToList();
            var offspring = crossover.Cross(parents.ToParentPairs(), random, problem.SearchSpace, problem);
            offspring = mutator.Mutate(offspring, random, problem.SearchSpace, problem);
            var fitnesses = evaluator.Evaluate(offspring, random, problem.SearchSpace, problem);
            var offspringPopulation = Population.From(offspring, fitnesses).EvaluatedCandidates;
            var newPopulation = ElitismReplacer<TCandidate>.Replace(oldPopulation, offspringPopulation, problem.Objective, populationSize, elites);
            return new PopulationState<TCandidate> { Population = Population.From(newPopulation) };
        }
    }
}

public record GeneticAlgorithm<TCandidate, TSearchSpace> : GeneticAlgorithm<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public record GeneticAlgorithm<TCandidate> : GeneticAlgorithm<TCandidate, ISearchSpace<TCandidate>>;

public static class GeneticAlgorithm
{
    public static GeneticAlgorithm<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(
        ICreator<TCandidate, TSearchSpace, TProblem> creator, ICrossover<TCandidate, TSearchSpace, TProblem> crossover, IMutator<TCandidate, TSearchSpace, TProblem> mutator,
        double mutationRate,
        ISelector<TCandidate, TSearchSpace, TProblem> selector, int populationSize,
        IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
        int elites = 1,
        IInterceptor<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor = null
    )
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        return new GeneticAlgorithm<TCandidate, TSearchSpace, TProblem>
        {
            Creator = creator,
            Crossover = crossover,
            Mutator = mutator,
            MutationRate = mutationRate,
            Selector = selector,
            Elites = elites,
            PopulationSize = populationSize,
            Evaluator = evaluator,
            Interceptor = interceptor
        };
    }

    public static GeneticAlgorithmBuilder<TCandidate, TSearchSpace, TProblem> GetBuilder<TCandidate, TSearchSpace, TProblem>(
        ICreator<TCandidate, TSearchSpace, TProblem> creator,
        ICrossover<TCandidate, TSearchSpace, TProblem> crossover,
        IMutator<TCandidate, TSearchSpace, TProblem> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        return new()
        {
            Mutator = mutator,
            Crossover = crossover,
            Creator = creator
        };
    }
}

using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record GeneticAlgorithm<TCandidate, TSearchSpace, TProblem>
  : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>, GeneticAlgorithm<TCandidate, TSearchSpace, TProblem>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public new sealed class ExecutionState
      : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>, ExecutionState>.ExecutionState
    {
        public required ICreatorInstance<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
        public required ICrossoverInstance<TCandidate, TSearchSpace, TProblem> Crossover { get; init; }
        public required IMutatorInstance<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
        public required ISelectorInstance<TCandidate, TSearchSpace, TProblem> Selector { get; init; }
        public ITerminatorInstance<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? Terminator { get; init; }
    }

    public required int PopulationSize { get; init; }
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem> Crossover { get; init; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
    public ITerminator<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? Terminator { get; init; }
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

    protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
    {
        var effectiveMutator = MutationRate >= 1.0
          ? Mutator
          : Mutator.WithRate(MutationRate);

        return new ExecutionState
        {
            Evaluator = resolver.Resolve(Evaluator),
            Interceptor = Interceptor is not null ? resolver.Resolve(Interceptor) : null,
            Creator = resolver.Resolve(Creator),
            Crossover = resolver.Resolve(Crossover),
            Mutator = resolver.Resolve(effectiveMutator),
            Selector = resolver.Resolve(Selector),
            Terminator = Terminator is not null ? resolver.Resolve(Terminator) : null
        };
    }

    protected override bool HasCompleted(
      int yieldedStateCount,
      PopulationState<TCandidate>? previousState,
      ExecutionState executionState,
      TProblem problem)
    {
        return MaximumGenerations is not null && yieldedStateCount >= MaximumGenerations.Value;
    }

    protected override bool IsTerminalState(
      PopulationState<TCandidate> state,
      int yieldedStateCount,
      PopulationState<TCandidate>? previousState,
      ExecutionState executionState,
      TProblem problem)
    {
        return executionState.Terminator?.IsTerminalState(state, problem.SearchSpace, problem) == true;
    }

    protected override PopulationState<TCandidate> ExecuteStep(
      PopulationState<TCandidate>? previousState,
      ExecutionState executionState,
      TProblem problem,
      IRandomNumberGenerator random)
    {
        if (previousState is null)
        {
            var initialSolutions = executionState.Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
            var initialFitnesses = executionState.Evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
            return new PopulationState<TCandidate>
            {
                Population = Population.From(initialSolutions, initialFitnesses)
            };
        }

        var oldPopulation = previousState.Population.EvaluatedCandidates;
        var offspringSize = PopulationSize * 2;

        var parents = executionState.Selector.Select(oldPopulation, problem.Objective, offspringSize, random, problem.SearchSpace, problem)
          .Select(x => x.Candidate)
          .ToList();

        var offspring = executionState.Crossover.Cross(parents.ToParentPairs(), random, problem.SearchSpace, problem);
        offspring = executionState.Mutator.Mutate(offspring, random, problem.SearchSpace, problem);
        var fitnesses = executionState.Evaluator.Evaluate(offspring, random, problem.SearchSpace, problem);
        var offspringPopulation = Population.From(offspring, fitnesses).EvaluatedCandidates;

        var newPopulation = ElitismReplacer<TCandidate>.Replace(oldPopulation, offspringPopulation, problem.Objective, PopulationSize, Elites);

        return new PopulationState<TCandidate>
        {
            Population = Population.From(newPopulation)
        };
    }
}

public record GeneticAlgorithm<TCandidate, TSearchSpace> : GeneticAlgorithm<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TCandidate>;

public record GeneticAlgorithm<TCandidate> : GeneticAlgorithm<TCandidate, ISearchSpace<TCandidate>>;

public static class GeneticAlgorithm
{
    public static GeneticAlgorithm<TG, TS, TP> Create<TG, TS, TP>(
      ICreator<TG, TS, TP> creator, ICrossover<TG, TS, TP> crossover, IMutator<TG, TS, TP> mutator,
      double mutationRate,
      ISelector<TG, TS, TP> selector, int populationSize,
      IEvaluator<TG, TS, TP> evaluator,
      int elites = 1,
      IInterceptor<TG, TS, TP, PopulationState<TG>>? interceptor = null
    )
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
    {
        return new GeneticAlgorithm<TG, TS, TP>
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

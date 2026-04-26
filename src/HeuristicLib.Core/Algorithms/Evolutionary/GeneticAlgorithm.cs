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

public record GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public new sealed class ExecutionState
    : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>, ExecutionState>.ExecutionState
  {
    public required ICreatorInstance<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
    public required ICrossoverInstance<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
    public required IMutatorInstance<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
    public required ISelectorInstance<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
  }

  public required int PopulationSize { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public int Elites { get; init; } = 1;

  public double MutationRate
  {
    get;
    init => field = value is >= 0.0 and <= 1.0 ? value : throw new ArgumentOutOfRangeException(nameof(MutationRate), "MutationRate must be in [0, 1].");
  } = 0.1;

  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }

  protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
  {
    var effectiveMutator = MutationRate >= 1.0
      ? Mutator
      : Mutator.WithRate(MutationRate);

    return new ExecutionState {
      Evaluator = resolver.Resolve(Evaluator),
      Interceptor = Interceptor is not null ? resolver.Resolve(Interceptor) : null,
      Creator = resolver.Resolve(Creator),
      Crossover = resolver.Resolve(Crossover),
      Mutator = resolver.Resolve(effectiveMutator),
      Selector = resolver.Resolve(Selector)
    };
  }

  protected override PopulationState<TGenotype> ExecuteStep(
    PopulationState<TGenotype>? previousState,
    ExecutionState executionState,
    TProblem problem,
    IRandomNumberGenerator random)
  {
    if (previousState is null) {
      var initialSolutions = executionState.Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
      var initialFitnesses = executionState.Evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
      return new PopulationState<TGenotype> {
        Population = Population.From(initialSolutions, initialFitnesses)
      };
    }

    var oldPopulation = previousState.Population.Solutions;
    var offspringSize = PopulationSize * 2;

    var parents = executionState.Selector.Select(oldPopulation, problem.Objective, offspringSize, random, problem.SearchSpace, problem)
      .Select(x => x.Genotype)
      .ToList();

    var offspring = executionState.Crossover.Cross(parents.ToParentPairs(), random, problem.SearchSpace, problem);
    offspring = executionState.Mutator.Mutate(offspring, random, problem.SearchSpace, problem);
    var fitnesses = executionState.Evaluator.Evaluate(offspring, random, problem.SearchSpace, problem);
    var offspringPopulation = Population.From(offspring, fitnesses).Solutions;

    var newPopulation = ElitismReplacer<TGenotype>.Replace(oldPopulation, offspringPopulation, problem.Objective, PopulationSize, Elites);

    return new PopulationState<TGenotype> {
      Population = Population.From(newPopulation)
    };
  }
}

public record GeneticAlgorithm<TGenotype, TSearchSpace> : GeneticAlgorithm<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>;

public record GeneticAlgorithm<TGenotype> : GeneticAlgorithm<TGenotype, ISearchSpace<TGenotype>>;

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
    return new GeneticAlgorithm<TG, TS, TP> {
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

  public static GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> GetBuilder<TGenotype, TSearchSpace, TProblem>(
    ICreator<TGenotype, TSearchSpace, TProblem> creator,
    ICrossover<TGenotype, TSearchSpace, TProblem> crossover,
    IMutator<TGenotype, TSearchSpace, TProblem> mutator)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    return new() {
      Mutator = mutator,
      Crossover = crossover,
      Creator = creator
    };
  }
}

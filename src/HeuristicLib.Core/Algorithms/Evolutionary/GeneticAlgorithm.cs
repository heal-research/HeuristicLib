using HEAL.HeuristicLib.AlgorithmExecutions;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Variations;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
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
  //public IReplacer<TGenotype, TSearchSpace, TProblem> Replacer { get; init; } = new ElitismReplacer<TGenotype>(1);

  public override EvolutionaryAlgorithmExecution<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    // ToDo: think about if this is the right way or if we break the instantiation process because we create a new operator here.
    var mutator = MutationRate >= 1.0
      ? Mutator
      : Mutator.WithRate(MutationRate);
    var crossover = Crossover; // ToDo: crossover probability

    var variation = new CrossoverAndMutationVariation<TGenotype, TSearchSpace, TProblem>(crossover, mutator);

    var replacer = new ElitismReplacer<TGenotype>(Elites);

    return new EvolutionaryAlgorithmExecution<TGenotype, TSearchSpace, TProblem>(
      PopulationSize,
      offspringSize: PopulationSize * 2,
      instanceRegistry.Resolve(Creator),
      instanceRegistry.Resolve(Selector),
      instanceRegistry.Resolve(variation),
      instanceRegistry.Resolve(replacer),
      instanceRegistry.Resolve(Evaluator),
      Interceptor is not null ? instanceRegistry.Resolve(Interceptor) : null
    );
  }
}

// ToDo: Do we want this, and if yes, do we want it for all algorithms?
public record GeneticAlgorithm<TGenotype, TSearchSpace> : GeneticAlgorithm<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> where TSearchSpace : class, ISearchSpace<TGenotype>;

public record GeneticAlgorithm<TGenotype> : GeneticAlgorithm<TGenotype, ISearchSpace<TGenotype>>;

public static class GeneticAlgorithm
{
  public static GeneticAlgorithm<TG, TS, TP> Create<TG, TS, TP>(
    ICreator<TG, TS, TP> creator, ICrossover<TG, TS, TP> crossover, IMutator<TG, TS, TP> mutator,
    double mutationRate,
    ISelector<TG, TS, TP> selector, int populationSize,
    IEvaluator<TG, TS, TP> evaluator,
    int elites = 1,
    Interceptor<TG, TS, TP, PopulationState<TG>>? interceptor = null
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
      //Replacer = replacer,
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

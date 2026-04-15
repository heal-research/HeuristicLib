using System.ComponentModel.DataAnnotations;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Variations;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationState<TGenotype>, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>.State>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public required int PopulationSize { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }

  public int Elites { get; init; } = 1;

  public double MutationRate
  {
    get;
    init => field = value is >= 0.0 and <= 1.0 ? value : throw new ArgumentOutOfRangeException(nameof(MutationRate), "MutationRate must be in [0, 1].");
  } = 0.1;

  protected override State CreateInitialState(ExecutionInstanceRegistry instanceRegistry) => new State(instanceRegistry, this);

  protected override PopulationState<TGenotype> ExecuteStep(State state, PopulationState<TGenotype>? previousState, TProblem problem, IRandomNumberGenerator random)
  {
    if (previousState is null) {
      var initialSolutions = state.Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
      var initialFitnesses = state.Evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
      return new PopulationState<TGenotype> {
        Population = Population.From(initialSolutions, initialFitnesses)
      };
    }

    var oldPopulation = previousState.Population.Solutions;
    var parentPairs = state.Selector.Select(oldPopulation, problem.Objective, PopulationSize * 2, random, problem.SearchSpace, problem).ToParents(problem.Objective);
    var offspring = state.Crossover.Cross(parentPairs, random, problem.SearchSpace, problem);
    offspring = state.Mutator.Mutate(offspring, random, problem.SearchSpace, problem);
    var fitnesses = state.Evaluator.Evaluate(offspring, random, problem.SearchSpace, problem);
    var offspringPopulation = Population.From(offspring, fitnesses).Solutions;

    var newPopulation = state.Replacer.Replace(oldPopulation, offspringPopulation, problem.Objective, PopulationSize, random, problem.SearchSpace, problem);

    return new PopulationState<TGenotype> {
      Population = Population.From(newPopulation)
    };
  }

  public class State : IterativeAlgorithmState
  {
    public ICreatorInstance<TGenotype, TSearchSpace, TProblem> Creator { get; }
    public ICrossoverInstance<TGenotype, TSearchSpace, TProblem> Crossover { get; }
    public IMutatorInstance<TGenotype, TSearchSpace, TProblem> Mutator { get; }
    public ISelectorInstance<TGenotype, TSearchSpace, TProblem> Selector { get; }
    public IReplacerInstance<TGenotype, TSearchSpace, TProblem> Replacer { get; }

    public State(ExecutionInstanceRegistry instanceRegistry, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem> algorithm) : base(instanceRegistry, algorithm)
    {
      Creator = instanceRegistry.Resolve(algorithm.Creator);
      Crossover = instanceRegistry.Resolve(algorithm.Crossover); //ToDo: crossover probability
      Mutator = instanceRegistry.Resolve(algorithm.Mutator.WithRate(algorithm.MutationRate));
      Selector = instanceRegistry.Resolve(algorithm.Selector);
      Replacer = instanceRegistry.Resolve(new ElitismReplacer<TGenotype>(algorithm.Elites));
    }
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

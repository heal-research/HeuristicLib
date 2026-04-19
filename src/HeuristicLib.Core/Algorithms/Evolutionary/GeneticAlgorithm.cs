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

  protected override PopulationState<TGenotype> ExecuteStep(
    PopulationState<TGenotype>? previousState,
    IOperatorExecutor executor,
    TProblem problem,
    IRandomNumberGenerator random)
  {
    if (previousState is null) {
      var initialSolutions = executor.Create(Creator, PopulationSize, random, problem.SearchSpace, problem);
      var initialFitnesses = executor.Evaluate(Evaluator, initialSolutions, random, problem.SearchSpace, problem);
      return new PopulationState<TGenotype> {
        Population = Population.From(initialSolutions, initialFitnesses)
      };
    }

    var oldPopulation = previousState.Population.Solutions;
    var offspringSize = PopulationSize * 2;
    var effectiveMutator = MutationRate >= 1.0
      ? Mutator
      : Mutator.WithRate(MutationRate);

    var parents = executor.Select(Selector, oldPopulation, problem.Objective, offspringSize, random, problem.SearchSpace, problem)
      .Select(x => x.Genotype)
      .ToList();

    var offspring = executor.Cross(Crossover, parents.ToParentPairs(), random, problem.SearchSpace, problem);
    offspring = executor.Mutate(effectiveMutator, offspring, random, problem.SearchSpace, problem);
    var fitnesses = executor.Evaluate(Evaluator, offspring, random, problem.SearchSpace, problem);
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

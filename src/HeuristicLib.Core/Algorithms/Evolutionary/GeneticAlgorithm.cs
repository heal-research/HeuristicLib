using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public class GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : Algorithm<TGenotype, TSearchSpace, TProblem, PopulationIterationState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TGenotype : class 
{
  public required int PopulationSize { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }

  public double MutationRate {
    get;
    init => field = value is >= 0.0 and <= 1.0 ? value : throw new ArgumentOutOfRangeException(nameof(MutationRate), "MutationRate must be in [0, 1].");
  } = 0.1;

  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
  public required IReplacer<TGenotype, TSearchSpace, TProblem> Replacer { get; init; }

  public override PopulationIterationState<TGenotype> ExecuteStep(TProblem problem, PopulationIterationState<TGenotype>? previousState, IRandomNumberGenerator random) {
    if (previousState == null) {
      var initialSolutions = Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
      var initialFitnesses = Evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
      return new PopulationIterationState<TGenotype> {
        Population = Population.From(initialSolutions, initialFitnesses), 
        CurrentIteration = 0
      };
    }
    
    var offspringCount = Replacer.GetOffspringCount(PopulationSize);
    var oldPopulation = previousState.Population.Solutions;
    var parents = Selector.Select(oldPopulation, problem.Objective, offspringCount * 2, random, problem.SearchSpace, problem).ToGenotypePairs();
    var population = Crossover.Cross(parents, random, problem.SearchSpace, problem);

    population = MutationRate >= 1.0
      ? Mutator.Mutate(population, random, problem.SearchSpace, problem)
      : Mutator.WithRate(MutationRate).Mutate(population, random, problem.SearchSpace, problem);

    var fitnesses = Evaluator.Evaluate(population, random, problem.SearchSpace, problem);
    var newPopulation = Replacer.Replace(oldPopulation, Population.From(population, fitnesses).Solutions, problem.Objective, random, problem.SearchSpace, problem);

    return new PopulationIterationState<TGenotype> {
      Population = Population.From(newPopulation),
      CurrentIteration = previousState.CurrentIteration + 1
    };
  }
}

public class GeneticAlgorithm<TGenotype, TSearchSpace> : GeneticAlgorithm<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> where TGenotype : class where TSearchSpace : class, ISearchSpace<TGenotype> ;

public class GeneticAlgorithm<TGenotype> : GeneticAlgorithm<TGenotype, ISearchSpace<TGenotype>> where TGenotype : class;

public static class GeneticAlgorithm {
  public static GeneticAlgorithm<TG, TS, TP> Create<TG, TS, TP>(
    ICreator<TG, TS, TP> creator, ICrossover<TG, TS, TP> crossover, IMutator<TG, TS, TP> mutator,
    double mutationRate,
    ISelector<TG, TS, TP> selector, IReplacer<TG, TS, TP> replacer, int populationSize,
    IEvaluator<TG, TS, TP> evaluator,
    Interceptor<TG, PopulationIterationState<TG>, TS, TP>? interceptor,
    ITerminator<TG, PopulationIterationState<TG>, TS, TP> terminator
  )
    where TG : class
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS> {
    return new GeneticAlgorithm<TG, TS, TP>() {
      Creator = creator,
      Crossover = crossover,
      Mutator = mutator,
      MutationRate = mutationRate,
      Selector = selector,
      Replacer = replacer,
      PopulationSize = populationSize,
      Evaluator = evaluator,
      Interceptor = interceptor,
      Terminator = terminator
    };
  }
  
  public static GeneticAlgorithmBuilder<TGenotype, TSearchSpace, TProblem> GetBuilder<TGenotype, TSearchSpace, TProblem>(
    ICreator<TGenotype, TSearchSpace, TProblem> creator,
    ICrossover<TGenotype, TSearchSpace, TProblem> crossover,
    IMutator<TGenotype, TSearchSpace, TProblem> mutator)
    where TSearchSpace : class, ISearchSpace<TGenotype> 
    where TProblem : class, IProblem<TGenotype, TSearchSpace> 
    where TGenotype : class => new() 
  {
    Mutator = mutator,
    Crossover = crossover,
    Creator = creator
  };
}

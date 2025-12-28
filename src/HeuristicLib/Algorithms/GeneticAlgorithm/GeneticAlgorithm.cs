using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Replacer;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public class GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, PopulationIterationResult<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TGenotype : class {
  public required int PopulationSize { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
  public required IReplacer<TGenotype, TSearchSpace, TProblem> Replacer { get; init; }

  public override PopulationIterationResult<TGenotype> ExecuteStep(TProblem problem, TSearchSpace searchSpace, PopulationIterationResult<TGenotype>? previousIterationResult, IRandomNumberGenerator random) {
    if (previousIterationResult == null)
      return new PopulationIterationResult<TGenotype>(CreateInitialPopulation(problem, searchSpace, random, PopulationSize));
    var offspringCount = Replacer.GetOffspringCount(PopulationSize);
    var oldPopulation = previousIterationResult.Population.Solutions;
    var parents = Selector.Select(oldPopulation, problem.Objective, offspringCount * 2, random, searchSpace, problem).ToGenotypePairs();
    var population = Crossover.Cross(parents, random, searchSpace, problem);
    population = Mutator.Mutate(population, random, searchSpace, problem);
    var fitnesses = Evaluator.Evaluate(population, random, searchSpace, problem);
    var newPopulation = Replacer.Replace(oldPopulation, Population.From(population, fitnesses).Solutions, problem.Objective, random, searchSpace, problem);

    return new PopulationIterationResult<TGenotype>(Population.From(newPopulation));
  }

  public class Builder : PopulationBasedAlgorithmBuilder<
                           TGenotype,
                           TSearchSpace,
                           TProblem,
                           PopulationIterationResult<TGenotype>,
                           GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>,
                         IMutatorPrototype<TGenotype, TSearchSpace, TProblem>,
                         ICrossoverPrototype<TGenotype, TSearchSpace, TProblem> {
    public double MutationRate { get; set; } = 0.05;
    public int Elites { get; set; } = 1;
    public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; set; }
    public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; set; }

    public GeneticAlgorithm<TGenotype, TSearchSpace, TProblem> Create() {
      return new GeneticAlgorithm<TGenotype, TSearchSpace, TProblem> {
        AlgorithmRandom = SystemRandomNumberGenerator.Default(RandomSeed),
        PopulationSize = PopulationSize,
        Creator = Creator,
        Crossover = Crossover,
        Selector = Selector,
        Evaluator = Evaluator,
        Replacer = new ElitismReplacer<TGenotype>(Elites),
        Terminator = Terminator,
        Interceptor = Interceptor,
        Mutator = Mutator.WithRate(MutationRate)
      };
    }

    public override GeneticAlgorithm<TGenotype, TSearchSpace, TProblem> BuildAlgorithm() => Create();
  }
}

public class GeneticAlgorithm<TGenotype, TSearchSpace> : GeneticAlgorithm<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> where TSearchSpace : class, ISearchSpace<TGenotype> where TGenotype : class;

public class GeneticAlgorithm<TGenotype> : GeneticAlgorithm<TGenotype, ISearchSpace<TGenotype>> where TGenotype : class;

public static class GeneticAlgorithm {
  public static GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>.Builder GetBuilder<TGenotype, TSearchSpace, TProblem>(
    ICreator<TGenotype, TSearchSpace, TProblem> creator,
    ICrossover<TGenotype, TSearchSpace, TProblem> crossover,
    IMutator<TGenotype, TSearchSpace, TProblem> mutator)
    where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> where TGenotype : class => new() {
    Mutator = mutator,
    Crossover = crossover,
    Creator = creator
  };
}

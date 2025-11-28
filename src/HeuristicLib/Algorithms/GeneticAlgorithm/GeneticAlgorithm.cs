using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Replacer;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public class GeneticAlgorithm<TGenotype, TEncoding, TProblem>
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, PopulationIterationResult<TGenotype>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public required int PopulationSize { get; init; }
  public required ICreator<TGenotype, TEncoding, TProblem> Creator { get; init; }
  public required ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TEncoding, TProblem> Mutator { get; init; }
  public required ISelector<TGenotype, TEncoding, TProblem> Selector { get; init; }
  public required IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; init; }
  public required IReplacer<TGenotype, TEncoding, TProblem> Replacer { get; init; }

  public override PopulationIterationResult<TGenotype> ExecuteStep(TProblem problem, TEncoding searchSpace, PopulationIterationResult<TGenotype>? previousIterationResult, IRandomNumberGenerator random) {
    return previousIterationResult switch {
      null => ExecuteInitialization(problem, searchSpace, random),
      _ => ExecuteGeneration(problem, searchSpace, previousIterationResult, random)
    };
  }

  protected virtual PopulationIterationResult<TGenotype> ExecuteInitialization(TProblem problem, TEncoding searchSpace, IRandomNumberGenerator random) {
    var population = Creator.Create(PopulationSize, random, searchSpace, problem);
    var objectives = Evaluator.Evaluate(population, random, searchSpace, problem);
    return new PopulationIterationResult<TGenotype>(Population.From(population, objectives));
  }

  protected virtual PopulationIterationResult<TGenotype> ExecuteGeneration(TProblem problem, TEncoding searchSpace, PopulationIterationResult<TGenotype> previousGenerationResult, IRandomNumberGenerator random) {
    var offspringCount = Replacer.GetOffspringCount(PopulationSize);
    var oldPopulation = previousGenerationResult.Population.Solutions;
    var parents = Selector.Select(oldPopulation, problem.Objective, offspringCount * 2, random, searchSpace, problem).ToGenotypePairs();
    var population = Crossover.Cross(parents, random, searchSpace, problem);
    population = Mutator.Mutate(population, random, searchSpace, problem);
    var fitnesses = Evaluator.Evaluate(population, random, searchSpace, problem);
    var newPopulation = Replacer.Replace(oldPopulation, Population.From(population, fitnesses).Solutions, problem.Objective, random, searchSpace, problem);

    return new PopulationIterationResult<TGenotype>(Population.From(newPopulation));
  }

  public class Builder : PopulationBasedAlgorithmBuilder<
                           TGenotype,
                           TEncoding,
                           TProblem,
                           PopulationIterationResult<TGenotype>,
                           GeneticAlgorithm<TGenotype, TEncoding, TProblem>>,
                         IMutatorPrototype<TGenotype, TEncoding, TProblem>,
                         ICrossoverPrototype<TGenotype, TEncoding, TProblem> {
    public double MutationRate { get; set; } = 0.05;
    public int Elites { get; set; } = 1;
    public required IMutator<TGenotype, TEncoding, TProblem> Mutator { get; set; }
    public required ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; set; }

    public GeneticAlgorithm<TGenotype, TEncoding, TProblem> Create() {
      return new GeneticAlgorithm<TGenotype, TEncoding, TProblem> {
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

    public override GeneticAlgorithm<TGenotype, TEncoding, TProblem> BuildAlgorithm() => Create();
  }
}

public class GeneticAlgorithm<TGenotype, TEncoding> : GeneticAlgorithm<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> where TEncoding : class, IEncoding<TGenotype>;

public class GeneticAlgorithm<TGenotype> : GeneticAlgorithm<TGenotype, IEncoding<TGenotype>>;

public static class GeneticAlgorithm {
  public static GeneticAlgorithm<TGenotype, TEncoding, TProblem>.Builder GetBuilder<TGenotype, TEncoding, TProblem>(
    ICreator<TGenotype, TEncoding, TProblem> creator,
    ICrossover<TGenotype, TEncoding, TProblem> crossover,
    IMutator<TGenotype, TEncoding, TProblem> mutator)
    where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding>
    => new() {
      Mutator = mutator,
      Crossover = crossover,
      Creator = creator
    };
}

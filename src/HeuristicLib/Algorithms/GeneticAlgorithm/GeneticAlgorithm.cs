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

public class GeneticAlgorithm<TGenotype, TEncoding, TProblem>(
  int populationSize,
  ICreator<TGenotype, TEncoding, TProblem> creator,
  ICrossover<TGenotype, TEncoding, TProblem> crossover,
  IMutator<TGenotype, TEncoding, TProblem> mutator,
  double mutationRate,
  ISelector<TGenotype, TEncoding, TProblem> selector,
  IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
  int elites,
  int? randomSeed,
  ITerminator<TGenotype, PopulationIterationResult<TGenotype>, TEncoding, TProblem> terminator,
  IInterceptor<TGenotype, PopulationIterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null)
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, PopulationIterationResult<TGenotype>>(terminator, randomSeed, interceptor)
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public int PopulationSize { get; } = populationSize;
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; init; } = creator;
  public ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; } = crossover;
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; } = mutator;
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; } = selector;
  public IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; } = evaluator;
  private readonly MultiMutator<TGenotype, TEncoding, TProblem> internalMutator = new([mutator, new NoChangeMutator<TGenotype>()], [mutationRate, 1 - mutationRate]);
  private readonly ElitismReplacer<TGenotype> internalReplacer = new(elites);

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
    var offspringCount = internalReplacer.GetOffspringCount(PopulationSize);
    var oldPopulation = previousGenerationResult.Population.Solutions;
    var parents = Selector.Select(oldPopulation, problem.Objective, offspringCount * 2, random, searchSpace, problem).ToGenotypePairs();
    var population = Crossover.Cross(parents, random, searchSpace, problem);
    population = internalMutator.Mutate(population, random, searchSpace, problem);
    var fitnesses = Evaluator.Evaluate(population, random, searchSpace, problem);
    var newPopulation = internalReplacer.Replace(oldPopulation, Population.From(population, fitnesses).Solutions, problem.Objective, random);

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
      return new(PopulationSize, Creator, Crossover, Mutator, MutationRate, Selector, Evaluator, Elites, RandomSeed, Terminator, Interceptor);
    }

    public override GeneticAlgorithm<TGenotype, TEncoding, TProblem> BuildAlgorithm() => Create();
  }
}

public class GeneticAlgorithm<TGenotype, TEncoding>(
  int populationSize,
  ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> creator,
  ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> crossover,
  IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> mutator,
  double mutationRate,
  ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> selector,
  IEvaluator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> evaluator,
  int elites,
  int randomSeed,
  ITerminator<TGenotype, PopulationIterationResult<TGenotype>, TEncoding, IProblem<TGenotype, TEncoding>> terminator,
  IInterceptor<TGenotype, PopulationIterationResult<TGenotype>, TEncoding, IProblem<TGenotype, TEncoding>>? interceptor = null)
  : GeneticAlgorithm<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>(populationSize, creator, crossover, mutator,
    mutationRate, selector, evaluator, elites, randomSeed, terminator, interceptor)
  where TEncoding : class, IEncoding<TGenotype>;

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

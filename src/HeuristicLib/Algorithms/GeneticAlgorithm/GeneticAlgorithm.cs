using System.Text;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Replacer;
using HEAL.HeuristicLib.Operators.Selector;
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
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, PopulationResult<TGenotype>, PopulationIterationResult<TGenotype>>(terminator, randomSeed, interceptor)
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public int PopulationSize { get; } = populationSize;
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; } = creator;
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

  protected override PopulationResult<TGenotype> FinalizeResult(PopulationIterationResult<TGenotype> iterationResult, TProblem problem) => new(iterationResult.Population);
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
  IInterceptor<TGenotype, PopulationIterationResult<TGenotype>, TEncoding, IProblem<TGenotype, TEncoding>>?
    interceptor = null)
  : GeneticAlgorithm<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>(populationSize, creator, crossover, mutator,
    mutationRate, selector, evaluator, elites, randomSeed, terminator, interceptor)
  where TEncoding : class, IEncoding<TGenotype>;

public static class GeneticAlgorithm {
  public class Prototype<TGenotype, TEncoding, TProblem>(
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
    : PopulationBasedAlgorithmPrototype<TGenotype, TEncoding, TProblem, PopulationIterationResult<TGenotype>>(populationSize, creator, crossover,
      mutator, selector, evaluator, randomSeed, terminator, interceptor) where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding> {
    public double MutationRate { get; set; } = mutationRate;
    public int Elites { get; set; } = elites;
  }

  public static Prototype<TGenotype, TEncoding, TProblem> CreatePrototype<TGenotype, TEncoding, TProblem>(
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
    where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding>
    => new(populationSize, creator, crossover, mutator, mutationRate, selector, evaluator, elites, randomSeed, terminator, interceptor);

  public static GeneticAlgorithm<TGenotype, TEncoding, TProblem> CreateAlgorithm<TGenotype, TEncoding, TProblem>(this Prototype<TGenotype, TEncoding, TProblem> proto) where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
    return Create(proto.PopulationSize, proto.Creator, proto.Crossover, proto.Mutator, proto.MutationRate, proto.Selector, proto.Evaluator, proto.Elites, proto.RandomSeed, proto.Terminator, proto.Interceptor);
  }

  public static GeneticAlgorithm<TGenotype, TEncoding, TProblem> Create<TGenotype, TEncoding, TProblem>(
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
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding> {
    return new GeneticAlgorithm<TGenotype, TEncoding, TProblem>(populationSize, creator, crossover, mutator, mutationRate, selector, evaluator, elites, randomSeed, terminator, interceptor);
  }
}

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

namespace HEAL.HeuristicLib.Algorithms.NSGA2;

public class NSGA2<TGenotype, TEncoding, TProblem>(
  ITerminator<TGenotype, NSGA2IterationResult<TGenotype>, TEncoding, TProblem> terminator,
  IInterceptor<TGenotype, NSGA2IterationResult<TGenotype>, TEncoding, TProblem>? interceptor,
  int populationSize,
  ICreator<TGenotype, TEncoding, TProblem> creator,
  ICrossover<TGenotype, TEncoding, TProblem> crossover,
  IMutator<TGenotype, TEncoding, TProblem> mutator,
  double mutationRate,
  ISelector<TGenotype, TEncoding, TProblem> selector,
  IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
  int? randomSeed,
  bool dominateOnEquals) :
  IterativeAlgorithm<TGenotype, TEncoding, TProblem, NSGA2Result<TGenotype>, NSGA2IterationResult<TGenotype>>(terminator, randomSeed, interceptor)
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public int PopulationSize { get; } = populationSize;
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; } = creator;
  public ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; } = crossover;
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; } = mutator.WithRate(mutationRate);
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; } = selector;
  public IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; } = evaluator;
  public IReplacer<TGenotype, TEncoding, TProblem> Replacer { get; } = new ParetoCrowdingReplacer<TGenotype>(dominateOnEquals);

  public override NSGA2IterationResult<TGenotype> ExecuteStep(TProblem problem, TEncoding searchSpace, NSGA2IterationResult<TGenotype>? previousIterationResult, IRandomNumberGenerator random) {
    if (previousIterationResult == null) {
      var genotypes = Creator.Create(PopulationSize, random, searchSpace, problem);
      var objectiveValues = Evaluator.Evaluate(genotypes, random, searchSpace, problem);
      return new NSGA2IterationResult<TGenotype>(Population.From(genotypes, objectiveValues));
    }

    var offspringCount = Replacer.GetOffspringCount(PopulationSize);
    var parents = Selector.Select(previousIterationResult.Population.Solutions, problem.Objective, offspringCount * 2, random, searchSpace, problem).ToGenotypePairs();
    var children = Crossover.Cross(parents, random, searchSpace, problem);
    var mutants = Mutator.Mutate(children, random, searchSpace, problem);
    var newPop = Population.From(children, Evaluator.Evaluate(mutants, random, searchSpace, problem));
    var nextPop = Replacer.Replace(previousIterationResult.Population.Solutions, newPop.Solutions, problem.Objective, random, searchSpace, problem);
    return new NSGA2IterationResult<TGenotype>(Population.From(nextPop));
  }

  protected override NSGA2Result<TGenotype> FinalizeResult(NSGA2IterationResult<TGenotype> iterationResult, TProblem problem) {
    return new NSGA2Result<TGenotype>(iterationResult.Population);
  }
}

public static class NSGA2 {
  public class Prototype<TGenotype, TEncoding, TProblem>(
    ICreator<TGenotype, TEncoding, TProblem> creator,
    ICrossover<TGenotype, TEncoding, TProblem> crossover,
    IMutator<TGenotype, TEncoding, TProblem> mutator,
    ISelector<TGenotype, TEncoding, TProblem> selector,
    ITerminator<TGenotype, NSGA2IterationResult<TGenotype>, TEncoding, TProblem> terminator,
    IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
    int? randomSeed,
    int populationSize,
    double mutationRate,
    bool dominateOnEquals,
    IInterceptor<TGenotype, NSGA2IterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null)
    : PopulationBasedAlgorithmPrototype<TGenotype, TEncoding, TProblem, NSGA2IterationResult<TGenotype>>(populationSize, creator, crossover,
      mutator, selector, evaluator, randomSeed, terminator, interceptor) where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding> {
    public double MutationRate { get; set; } = mutationRate;
    public bool DominateOnEquals { get; set; } = dominateOnEquals;
  }

  public static Prototype<TGenotype, TEncoding, TProblem> CreatePrototype<TGenotype, TEncoding, TProblem>(ICreator<TGenotype, TEncoding, TProblem> creator,
                                                                                                          ICrossover<TGenotype, TEncoding, TProblem> crossover,
                                                                                                          IMutator<TGenotype, TEncoding, TProblem> mutator,
                                                                                                          ISelector<TGenotype, TEncoding, TProblem> selector,
                                                                                                          ITerminator<TGenotype, NSGA2IterationResult<TGenotype>, TEncoding, TProblem> terminator,
                                                                                                          IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
                                                                                                          int? randomSeed,
                                                                                                          int populationSize,
                                                                                                          double mutationRate,
                                                                                                          bool dominateOnEquals = true,
                                                                                                          IInterceptor<TGenotype, NSGA2IterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null)
    where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding>
    => new(creator, crossover, mutator, selector, terminator, evaluator, randomSeed, populationSize, mutationRate, dominateOnEquals, interceptor);

  public static NSGA2<TGenotype, TEncoding, TProblem> Create<TGenotype, TEncoding, TProblem>(this Prototype<TGenotype, TEncoding, TProblem> prototype)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding>
    => new(prototype.Terminator, prototype.Interceptor, prototype.PopulationSize, prototype.Creator, prototype.Crossover, prototype.Mutator, prototype.MutationRate, prototype.Selector, prototype.Evaluator, prototype.RandomSeed, prototype.DominateOnEquals);
}

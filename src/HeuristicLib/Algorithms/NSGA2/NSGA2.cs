using HEAL.HeuristicLib.Operators;
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
      var objectiveValues = Evaluator.Evaluate(genotypes, searchSpace, problem);
      return new NSGA2IterationResult<TGenotype>(Population.From(genotypes, objectiveValues));
    }

    var offspringCount = Replacer.GetOffspringCount(PopulationSize);
    var parents = Selector.Select(previousIterationResult.Population.Solutions, problem.Objective, offspringCount * 2, random, searchSpace, problem).ToGenotypePairs();
    var children = Crossover.Cross(parents, random, searchSpace, problem);
    var mutants = Mutator.Mutate(children, random, searchSpace, problem);
    var newPop = Population.From(children, Evaluator.Evaluate(mutants, searchSpace, problem));
    var nextPop = Replacer.Replace(previousIterationResult.Population.Solutions, newPop.Solutions, problem.Objective, random, searchSpace, problem);
    return new NSGA2IterationResult<TGenotype>(Population.From(nextPop));
  }

  protected override NSGA2Result<TGenotype> FinalizeResult(NSGA2IterationResult<TGenotype> iterationResult, TProblem problem) {
    return new NSGA2Result<TGenotype>(iterationResult.Population);
  }
}

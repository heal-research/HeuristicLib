using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms;

public abstract class Algorithm<TGenotype, TEncoding, TProblem, TAlgorithmResult> : IAlgorithm<TGenotype, TEncoding, TProblem, TAlgorithmResult>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TAlgorithmResult : class, IIterationResult {
  public abstract TAlgorithmResult Execute(TProblem problem, TEncoding? searchSpace = null, IRandomNumberGenerator? random = null);
  IIterationResult IAlgorithm<TGenotype, TEncoding, TProblem>.Execute(TProblem problem, TEncoding? searchSpace, IRandomNumberGenerator? random) => Execute(problem, searchSpace, random);
}

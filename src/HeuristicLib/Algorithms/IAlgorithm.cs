using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm<TGenotype, in TEncoding, in TProblem, out TAlgorithmResult> : IAlgorithm<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TAlgorithmResult : IIterationResult {
  new TAlgorithmResult Execute(TProblem problem, TEncoding? searchSpace = null, IRandomNumberGenerator? random = null);
}

public interface IAlgorithm<TGenotype, in TEncoding, in TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  IIterationResult Execute(TProblem problem, TEncoding? searchSpace = null, IRandomNumberGenerator? random = null);
}

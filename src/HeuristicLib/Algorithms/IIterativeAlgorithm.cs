using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms;

public interface IIterativeAlgorithm<TGenotype, in TEncoding, in TProblem, TIterationResult>
  : IAlgorithm<TGenotype, TEncoding, TProblem, TIterationResult>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TIterationResult : IIterationResult, IAlgorithmResult {
  TIterationResult ExecuteStep(TProblem problem, TEncoding searchSpace, TIterationResult? previousIterationResult, IRandomNumberGenerator random);
  IEnumerable<TIterationResult> ExecuteStreaming(TProblem problem, TEncoding? searchSpace = null, TIterationResult? previousIterationResult = default, IRandomNumberGenerator? random = null);
}

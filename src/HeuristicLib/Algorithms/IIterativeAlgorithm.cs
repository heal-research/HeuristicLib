using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms;

public interface IIterativeAlgorithm<TGenotype, in TEncoding, in TProblem, TAlgorithmResult, TIterationResult>
  : IAlgorithm<TGenotype, TEncoding, TProblem, TAlgorithmResult>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TAlgorithmResult : IAlgorithmResult<TGenotype>
  where TIterationResult : IIterationResult<TGenotype> {
  int CurrentIteration { get; }

  TAlgorithmResult Execute(TProblem problem, TEncoding? searchSpace = null, TIterationResult? previousIterationResult = default, IRandomNumberGenerator? random = null);
  TIterationResult ExecuteStep(TProblem problem, TEncoding? searchSpace = null, TIterationResult? previousIterationResult = default, IRandomNumberGenerator? random = null);
  IEnumerable<TIterationResult> ExecuteStreaming(TProblem problem, TEncoding? searchSpace = null, TIterationResult? previousIterationResult = default, IRandomNumberGenerator? random = null);
}

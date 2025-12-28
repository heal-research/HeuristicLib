using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IIterativeAlgorithm<TGenotype, in TSearchSpace, in TProblem, TIterationResult>
  : IAlgorithm<TGenotype, TSearchSpace, TProblem, TIterationResult>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TIterationResult : IIterationResult {
  TIterationResult ExecuteStep(TProblem problem, TSearchSpace searchSpace, TIterationResult? previousIterationResult, IRandomNumberGenerator random);
  IEnumerable<TIterationResult> ExecuteStreaming(TProblem problem, TSearchSpace? searchSpace = null, TIterationResult? previousIterationResult = default, IRandomNumberGenerator? random = null);
}

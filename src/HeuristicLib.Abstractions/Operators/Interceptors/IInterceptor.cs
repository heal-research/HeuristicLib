using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public interface IInterceptor<TGenotype, TIterationResult, in TSearchSpace, in TProblem>
  where TIterationResult : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem);
}

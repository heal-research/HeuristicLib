using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IIterativeAlgorithm<TCandidate, in TSearchSpace, in TProblem, TSearchState>
  : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>? Interceptor { get; }
}

using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithmBuilder;

#pragma warning disable S2326
public interface IAlgorithmBuilder<TCandidate, TSearchSpace, TProblem, TSearchState>
#pragma warning restore S2326
  : IAlgorithmBuilder
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState;

public interface IAlgorithmBuilder<TCandidate, TSearchSpace, TProblem, TSearchState, out TAlgorithm>
  : IAlgorithmBuilder<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TAlgorithm : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    TAlgorithm Build();
}

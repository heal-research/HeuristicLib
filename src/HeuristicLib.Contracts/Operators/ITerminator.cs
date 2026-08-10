using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators;

public interface ITerminator<TCandidate, in TSearchSpace, in TProblem, in TSearchState>
  : IOperator<ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState;

public interface ITerminatorInstance<TCandidate, in TSearchSpace, in TProblem, in TSearchState>
  : IOperatorInstance
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem);
}

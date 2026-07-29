using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators;

public interface IInterceptor<TCandidate, in TSearchSpace, in TProblem, TSearchState>
  : IOperator<IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public interface IInterceptorInstance<TCandidate, in TSearchSpace, in TProblem, TSearchState>
  : IOperatorInstance
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem);
}

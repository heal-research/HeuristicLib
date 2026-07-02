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
    // ToDo: think about really providing the previous state, as it implies some form of iteration and state storage (if the interceptor really needs the previous state, it can be stateful and store it on its own).
    TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem);
}

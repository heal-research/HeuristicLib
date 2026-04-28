using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators;

public interface IInterceptor<TGenotype, in TSearchSpace, in TProblem, TSearchState>
  : IOperator<IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TSearchState>>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>;

public interface IInterceptorInstance<TGenotype, in TSearchSpace, in TProblem, TSearchState>
  : IOperatorInstance
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  // ToDo: think about really providing the previous state, as it implies some form of iteration and state storage (if the interceptor really needs the previous state, it can be stateful and store it on its own).
  TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem);
}

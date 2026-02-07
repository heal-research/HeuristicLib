using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators;

// ToDo: think about unifying generic argument order. Maybe State first, then the usual triple afterwards.
public interface IInterceptor<TGenotype, TAlgorithmState, in TSearchSpace, in TProblem>
  : IOperator<IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
 }

public interface IInterceptorInstance<TGenotype, TAlgorithmState, in TSearchSpace, in TProblem>
  : IOperatorInstance
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  // ToDo: think about really providing the previous state, as it implies some form of iteration and state storage
  TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem);
}

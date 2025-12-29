using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public interface IInterceptor<TGenotype, TAlgorithmState, in TSearchSpace, in TProblem>
  where TAlgorithmState : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem);
}

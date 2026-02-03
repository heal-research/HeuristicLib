using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public interface IInterceptor<TGenotype, TAlgorithmState, in TSearchSpace, in TProblem>
  where TAlgorithmState : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  // ToDo: think about really providing the previous state, as it implies some form of iteration and state storage
  TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem);
}

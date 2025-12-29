using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public interface IInterceptor<TGenotype, TIterationState, in TSearchSpace, in TProblem>
  where TIterationState : IIterationState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  TIterationState Transform(TIterationState currenTIterationState, TIterationState? previousIterationState, TSearchSpace searchSpace, TProblem problem);
}

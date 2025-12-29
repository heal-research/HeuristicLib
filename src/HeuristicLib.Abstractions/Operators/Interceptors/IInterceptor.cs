using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public interface IInterceptor<TGenotype, TIterationResult, in TSearchSpace, in TProblem>
  where TIterationResult : IIterationResult
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TSearchSpace searchSpace, TProblem problem);
}

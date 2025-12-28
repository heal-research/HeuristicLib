using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptor;

public interface IInterceptor<TGenotype, TIterationResult, in TSearchSpace, in TProblem>
  where TIterationResult : IIterationResult
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TSearchSpace searchSpace, TProblem problem);
}

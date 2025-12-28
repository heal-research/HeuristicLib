using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptor;

public static class MultiInterceptor {
  public static MultiInterceptor<TGenotype, TResult, TSearchSpace, TProblem>? Create<TGenotype, TResult, TSearchSpace, TProblem>(
    IInterceptor<TGenotype, TResult, TSearchSpace, TProblem>? interceptor)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TResult : IIterationResult {
    var list = new List<IInterceptor<TGenotype, TResult, TSearchSpace, TProblem>>();
    if (interceptor != null)
      list.Add(interceptor);
    return list.Count == 0 ? null : new MultiInterceptor<TGenotype, TResult, TSearchSpace, TProblem>(list);
  }
}

public class MultiInterceptor<TGenotype, TIterationResult, TSearchSpace, TProblem>(IEnumerable<IInterceptor<TGenotype, TIterationResult, TSearchSpace, TProblem>> interceptors) : Interceptor<TGenotype, TIterationResult, TSearchSpace, TProblem> where TIterationResult : IIterationResult where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public List<IInterceptor<TGenotype, TIterationResult, TSearchSpace, TProblem>> Interceptors { get; } = interceptors.ToList();

  public override TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TSearchSpace searchSpace, TProblem problem) {
    return Interceptors.Aggregate(currentIterationResult, (current, interceptor) => interceptor.Transform(current, previousIterationResult, searchSpace, problem));
  }
}

using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public static class MultiInterceptor {
  public static MultiInterceptor<TGenotype, TResult, TSearchSpace, TProblem>? Create<TGenotype, TResult, TSearchSpace, TProblem>(
    IInterceptor<TGenotype, TResult, TSearchSpace, TProblem>? interceptor)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TResult : IIterationState {
    var list = new List<IInterceptor<TGenotype, TResult, TSearchSpace, TProblem>>();
    if (interceptor != null)
      list.Add(interceptor);
    return list.Count == 0 ? null : new MultiInterceptor<TGenotype, TResult, TSearchSpace, TProblem>(list);
  }
}

public class MultiInterceptor<TGenotype, TIterationState, TSearchSpace, TProblem>(IEnumerable<IInterceptor<TGenotype, TIterationState, TSearchSpace, TProblem>> interceptors) : Interceptor<TGenotype, TIterationState, TSearchSpace, TProblem> where TIterationState : IIterationState where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public List<IInterceptor<TGenotype, TIterationState, TSearchSpace, TProblem>> Interceptors { get; } = interceptors.ToList();

  public override TIterationState Transform(TIterationState currenTIterationState, TIterationState? previousIterationState, TSearchSpace searchSpace, TProblem problem) {
    return Interceptors.Aggregate(currenTIterationState, (current, interceptor) => interceptor.Transform(current, previousIterationState, searchSpace, problem));
  }
}

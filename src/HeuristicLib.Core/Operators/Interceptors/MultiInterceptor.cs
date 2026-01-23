using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public static class MultiInterceptor
{
  public static MultiInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? Create<TGenotype, TAlgorithmState, TSearchSpace, TProblem>(
    IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? interceptor)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : IAlgorithmState
  {
    var list = new List<IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>>();
    if (interceptor != null) {
      list.Add(interceptor);
    }

    return list.Count == 0 ? null : new MultiInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>(list);
  }
}

public class MultiInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>(IEnumerable<IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> interceptors) : Interceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem> where TAlgorithmState : IAlgorithmState where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public List<IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> Interceptors { get; } = interceptors.ToList();

  public override TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem) => Interceptors.Aggregate(currentState, (current, interceptor) => interceptor.Transform(current, previousState, searchSpace, problem));
}

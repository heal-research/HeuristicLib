using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public static class MultiInterceptor
{
  public static MultiInterceptor<TGenotype, TResult, TSearchSpace, TProblem>? Create<TGenotype, TResult, TSearchSpace, TProblem>(
    IInterceptor<TGenotype, TResult, TSearchSpace, TProblem>? interceptor)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TResult : IAlgorithmState
  {
    var list = new List<IInterceptor<TGenotype, TResult, TSearchSpace, TProblem>>();
    if (interceptor != null) {
      list.Add(interceptor);
    }

    return list.Count == 0 ? null : new MultiInterceptor<TGenotype, TResult, TSearchSpace, TProblem>(list);
  }
}

public class MultiInterceptor<TGenotype, TIterationResult, TSearchSpace, TProblem>(IEnumerable<IInterceptor<TGenotype, TIterationResult, TSearchSpace, TProblem>> interceptors) : Interceptor<TGenotype, TIterationResult, TSearchSpace, TProblem> where TIterationResult : IAlgorithmState where TSearchSpace : class, ISearchSpace<TGenotype> where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public List<IInterceptor<TGenotype, TIterationResult, TSearchSpace, TProblem>> Interceptors { get; } = interceptors.ToList();

  public override TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem) => Interceptors.Aggregate(currentIterationResult, func: (current, interceptor) => interceptor.Transform(current, previousIterationResult, random, encoding, problem));
}

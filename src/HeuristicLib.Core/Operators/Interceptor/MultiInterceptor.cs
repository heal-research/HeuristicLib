using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Interceptor;

public static class MultiInterceptor {
  public static MultiInterceptor<TGenotype, TResult, TEncoding, TProblem>? Create<TGenotype, TResult, TEncoding, TProblem>(
    IInterceptor<TGenotype, TResult, TEncoding, TProblem>? interceptor)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding>
    where TResult : IAlgorithmState {
    var list = new List<IInterceptor<TGenotype, TResult, TEncoding, TProblem>>();
    if (interceptor != null)
      list.Add(interceptor);
    return list.Count == 0 ? null : new MultiInterceptor<TGenotype, TResult, TEncoding, TProblem>(list);
  }
}

public class MultiInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>(IEnumerable<IInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>> interceptors) : Interceptor<TGenotype, TIterationResult, TEncoding, TProblem> where TIterationResult : IAlgorithmState where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
  public List<IInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>> Interceptors { get; } = interceptors.ToList();

  public override TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    return Interceptors.Aggregate(currentIterationResult, (current, interceptor) => interceptor.Transform(current, previousIterationResult, random, encoding, problem));
  }
}

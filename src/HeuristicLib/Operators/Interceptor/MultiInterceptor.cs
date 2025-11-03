using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Interceptor;

public static class MultiInterceptor {
  public static MultiInterceptor<TGenotype, TIterationResult, TEncoding, TProblem> Build<TGenotype, TIterationResult, TEncoding, TProblem>(IEnumerable<IInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>> interceptors) where TIterationResult : IIterationResult where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
    return new MultiInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>(interceptors);
  }
}

public class MultiInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>(IEnumerable<IInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>> interceptors) : Interceptor<TGenotype, TIterationResult, TEncoding, TProblem> where TIterationResult : IIterationResult where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
  public List<IInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>> Interceptors = interceptors.ToList();

  public override TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TEncoding encoding, TProblem problem) {
    return Interceptors.Aggregate(currentIterationResult, (current, interceptor) => interceptor.Transform(current, previousIterationResult, encoding, problem));
  }
}

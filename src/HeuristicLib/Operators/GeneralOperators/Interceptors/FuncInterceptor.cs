using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public class FuncInterceptor {
  public static FuncInterceptor<TGenotype, TResult> Build<TGenotype, TResult>(Action<TResult?, TResult> action) where TResult : IIterationResult<TGenotype> {
    return new FuncInterceptor<TGenotype, TResult>(action);
  }
}

public class FuncInterceptor<TGenotype, TResult>(Action<TResult?, TResult> action) : IInterceptor<TGenotype, TResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
  where TResult : IIterationResult<TGenotype> {
  public TResult Transform(TResult currentIterationResult, TResult? previousIterationResult, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    action.Invoke(previousIterationResult, currentIterationResult);
    return currentIterationResult;
  }
}

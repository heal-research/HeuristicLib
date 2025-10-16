using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public class FuncAnalyzer {
  public static FuncAnalyzer<TGenotype, TResult> Build<TGenotype, TResult>(Action<TResult?, TResult> action) where TResult : IIterationResult<TGenotype> {
    return new FuncAnalyzer<TGenotype, TResult>(action);
  }
}

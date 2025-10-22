using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public class FuncAnalyzer<TGenotype, TResult>(Action<TResult?, TResult> action) : IAnalyzer<TGenotype, TResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
  where TResult : IIterationResult {
  public void Analyze(TResult currentIterationResult, TResult? previousIterationResult, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) => action.Invoke(previousIterationResult, currentIterationResult);
}

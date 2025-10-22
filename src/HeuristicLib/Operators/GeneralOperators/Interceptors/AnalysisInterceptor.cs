using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public class AnalysisInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>(IEnumerable<IAnalyzer<TGenotype, TIterationResult, TEncoding, TProblem>> interceptors) : Interceptor<TGenotype, TIterationResult, TEncoding, TProblem> where TIterationResult : IIterationResult where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
  private readonly List<IAnalyzer<TGenotype, TIterationResult, TEncoding, TProblem>> Interceptors = interceptors.ToList();

  public override TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TEncoding encoding, TProblem problem) {
    foreach (var analyzer in Interceptors) {
      analyzer.Analyze(currentIterationResult, previousIterationResult, encoding, problem);
    }

    return currentIterationResult;
  }
}

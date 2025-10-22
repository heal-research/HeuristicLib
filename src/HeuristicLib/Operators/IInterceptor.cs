using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators;

public interface IInterceptor<TGenotype, TIterationResult, in TEncoding, in TProblem>
  where TIterationResult : IIterationResult
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TEncoding encoding, TProblem problem);
}

public interface IAnalyzer<in TGenotype, in TIterationResult, in TEncoding, in TProblem>
  where TIterationResult : IIterationResult
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  void Analyze(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TEncoding encoding, TProblem problem);
}

using System.Diagnostics;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms;

public abstract class IterativeAlgorithm<TGenotype, TEncoding, TProblem, TAlgorithmResult, TIterationResult>(ITerminator<TGenotype, TIterationResult, TEncoding, TProblem> terminator, IInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>? interceptor)
  : Algorithm<TGenotype, TEncoding, TProblem, TAlgorithmResult>, IIterativeAlgorithm<TGenotype, TEncoding, TProblem, TAlgorithmResult, TIterationResult>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TAlgorithmResult : IAlgorithmResult<TGenotype>
  where TIterationResult : IIterationResult<TGenotype> {
  public int CurrentIteration { get; protected set; }

  public ITerminator<TGenotype, TIterationResult, TEncoding, TProblem> Terminator { get; } = terminator;
  public IInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>? Interceptor { get; } = interceptor;

  public OperatorMetric TerminatorMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric InterceptorMetric { get; protected set; } = OperatorMetric.Zero;

  public abstract TIterationResult ExecuteStep(TProblem problem, TEncoding? searchSpace = null, TIterationResult? previousIterationResult = default, IRandomNumberGenerator? random = null);

  protected abstract TAlgorithmResult FinalizeResult(TIterationResult iterationResult, TProblem problem);

  public override TAlgorithmResult Execute(TProblem problem, TEncoding? searchSpace = null, IRandomNumberGenerator? random = null) {
    return Execute(problem, searchSpace, previousIterationResult: default, random);
  }

  public virtual TAlgorithmResult Execute(TProblem problem, TEncoding? searchSpace = null, TIterationResult? previousIterationResult = default, IRandomNumberGenerator? random = null) {
    TIterationResult? lastResult = ExecuteStreaming(problem, searchSpace, previousIterationResult).LastOrDefault();

    if (lastResult is null) {
      throw new InvalidOperationException("The algorithm did not produce any iteration result.");
    }

    return FinalizeResult(lastResult, problem);
  }

  public virtual IEnumerable<TIterationResult> ExecuteStreaming(TProblem problem, TEncoding? searchSpace = null, TIterationResult? previousIterationResult = default, IRandomNumberGenerator? random = null) {
    long start = Stopwatch.GetTimestamp();

    bool shouldContinue = previousIterationResult is null;
    if (previousIterationResult is not null) {
      long startTerminator = Stopwatch.GetTimestamp();
      shouldContinue = Terminator.ShouldContinue(previousIterationResult, previousIterationState: default, searchSpace ?? problem.SearchSpace, problem);
      long endTerminator = Stopwatch.GetTimestamp();
      TerminatorMetric += new OperatorMetric(1, Stopwatch.GetElapsedTime(startTerminator, endTerminator));
    }

    while (shouldContinue) {
      var newIterationResult = ExecuteStep(problem, searchSpace, previousIterationResult, random);

      if (Interceptor is not null) {
        long startInterceptor = Stopwatch.GetTimestamp();
        newIterationResult = Interceptor.Transform(newIterationResult, previousIterationResult, searchSpace ?? problem.SearchSpace, problem);
        long endInterceptor = Stopwatch.GetTimestamp();
        InterceptorMetric += new OperatorMetric(1, Stopwatch.GetElapsedTime(startInterceptor, endInterceptor));
      }

      long end = Stopwatch.GetTimestamp();

      CurrentIteration += 1;
      TotalExecutionTime += Stopwatch.GetElapsedTime(start, end);

      yield return newIterationResult;

      start = Stopwatch.GetTimestamp();

      long startTerminator = Stopwatch.GetTimestamp();
      shouldContinue = Terminator.ShouldContinue(newIterationResult, previousIterationResult, searchSpace ?? problem.SearchSpace, problem);
      long endTerminator = Stopwatch.GetTimestamp();
      TerminatorMetric += new OperatorMetric(1, Stopwatch.GetElapsedTime(startTerminator, endTerminator));

      previousIterationResult = newIterationResult;
    }
  }
}

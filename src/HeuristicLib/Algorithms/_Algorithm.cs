using System.Diagnostics;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithmResult<out TGenotype>
{
}

public interface IAlgorithm<TGenotype, in TEncoding, in TProblem, TAlgorithmResult>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
  where TAlgorithmResult : IAlgorithmResult<TGenotype>
{
  TimeSpan TotalExecutionTime { get; }
  OperatorMetric EvaluationsMetric { get; }
  
  TAlgorithmResult Execute(TProblem problem);
}


public abstract class Algorithm<TGenotype, TEncoding, TProblem, TAlgorithmResult> : IAlgorithm<TGenotype, TEncoding, TProblem, TAlgorithmResult>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
  where TAlgorithmResult : IAlgorithmResult<TGenotype>
{
  public TimeSpan TotalExecutionTime { get; protected set; } = TimeSpan.Zero;
  public OperatorMetric EvaluationsMetric { get; protected set; } = OperatorMetric.Zero;
  
  public abstract TAlgorithmResult Execute(TProblem problem);
}


public interface IIterationResult<out TGenotype>
{
}

public interface IIterativeAlgorithm<TGenotype, in TEncoding, in TProblem, TAlgorithmResult, TIterationResult>
  : IAlgorithm<TGenotype, TEncoding, TProblem, TAlgorithmResult>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
  where TAlgorithmResult : IAlgorithmResult<TGenotype>
  where TIterationResult : IIterationResult<TGenotype>
{
  int CurrentIteration { get; }
  
  TAlgorithmResult Execute(TProblem problem, TIterationResult? previousIterationResult);
  TIterationResult ExecuteStep(TProblem problem, TIterationResult? previousIterationResult = default);
  IEnumerable<TIterationResult> ExecuteStreaming(TProblem problem, TIterationResult? previousIterationResult = default);
}


public abstract class IterativeAlgorithm<TGenotype, TEncoding, TProblem, TAlgorithmResult, TIterationResult>
  : Algorithm<TGenotype, TEncoding, TProblem, TAlgorithmResult>, IIterativeAlgorithm<TGenotype, TEncoding, TProblem, TAlgorithmResult, TIterationResult>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
  where TAlgorithmResult : IAlgorithmResult<TGenotype>
  where TIterationResult : IIterationResult<TGenotype>
{
  public int CurrentIteration { get; protected set; }
  
  public ITerminator<TGenotype, TIterationResult, TEncoding, TProblem> Terminator { get; }
  public IInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>? Interceptor { get; }

  public OperatorMetric TerminatorMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric InterceptorMetric { get; protected set; } = OperatorMetric.Zero;
  

  protected IterativeAlgorithm(ITerminator<TGenotype, TIterationResult, TEncoding, TProblem> terminator, IInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>? interceptor) {
    Terminator = terminator;
    Interceptor = interceptor;
  }
  
  public abstract TIterationResult ExecuteStep(TProblem problem, TIterationResult? previousIterationResult = default);

  protected abstract TAlgorithmResult FinalizeResult(TIterationResult iterationResult, TProblem problem);
  
  public override TAlgorithmResult Execute(TProblem problem) {
    return Execute(problem, previousIterationResult: default);
  }

  public virtual TAlgorithmResult Execute(TProblem problem, TIterationResult? previousIterationResult) {
    TIterationResult? lastResult = ExecuteStreaming(problem, previousIterationResult).LastOrDefault();

    if (lastResult is null) {
      throw new InvalidOperationException("The algorithm did not produce any iteration result.");
    }

    return FinalizeResult(lastResult, problem);
  }

  public virtual IEnumerable<TIterationResult> ExecuteStreaming(TProblem problem, TIterationResult? previousIterationResult = default) {
    long start = Stopwatch.GetTimestamp();

    bool shouldContinue = previousIterationResult is null;
    if (previousIterationResult is not null) {
      long startTerminator = Stopwatch.GetTimestamp();
      shouldContinue = Terminator.ShouldContinue(previousIterationResult, previousIterationState: default, problem.Encoding, problem);
      long endTerminator = Stopwatch.GetTimestamp();
      TerminatorMetric += new OperatorMetric(1, Stopwatch.GetElapsedTime(startTerminator, endTerminator));
    }

    while (shouldContinue) {
      var newIterationResult = ExecuteStep(problem, previousIterationResult);

      if (Interceptor is not null) {
        long startInterceptor = Stopwatch.GetTimestamp();
        newIterationResult = Interceptor.Transform(newIterationResult, previousIterationResult, problem.Encoding, problem);
        long endInterceptor = Stopwatch.GetTimestamp();
        InterceptorMetric += new OperatorMetric(1, Stopwatch.GetElapsedTime(startInterceptor, endInterceptor));
      }

      long end = Stopwatch.GetTimestamp();

      CurrentIteration += 1;
      TotalExecutionTime += Stopwatch.GetElapsedTime(start, end);

      yield return newIterationResult;

      start = Stopwatch.GetTimestamp();

      long startTerminator = Stopwatch.GetTimestamp();
      shouldContinue = Terminator.ShouldContinue(newIterationResult, previousIterationResult, problem.Encoding, problem);
      long endTerminator = Stopwatch.GetTimestamp();
      TerminatorMetric += new OperatorMetric(1, Stopwatch.GetElapsedTime(startTerminator, endTerminator));

      previousIterationResult = newIterationResult;
    }
  }
}

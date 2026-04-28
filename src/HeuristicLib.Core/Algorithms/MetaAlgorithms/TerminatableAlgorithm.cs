using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

// ToDo: Think about if we really want to handle termination via decorators. Maybe as additional mechanism for algorithms that do not hav inner termination criterion, but otherwise, this feels overcomplicated.
public record TerminatableAlgorithm<TG, TS, TP, TSearchState>
  : Algorithm<TG, TS, TP, TSearchState, TerminatableAlgorithm<TG, TS, TP, TSearchState>.ExecutionState>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TSearchState : class, ISearchState
{
  public new sealed class ExecutionState
    : Algorithm<TG, TS, TP, TSearchState, ExecutionState>.ExecutionState
  {
    public required IAlgorithmInstance<TG, TS, TP, TSearchState> Algorithm { get; init; }
    public required ITerminatorInstance<TG, TS, TP, TSearchState> Terminator { get; init; }
  }

  public required IAlgorithm<TG, TS, TP, TSearchState> Algorithm { get; init; }
  public required ITerminator<TG, TS, TP, TSearchState> Terminator { get; init; }

  protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
  {
    return new ExecutionState {
      Evaluator = resolver.Resolve(Evaluator),
      Algorithm = resolver.Resolve(Algorithm),
      Terminator = resolver.Resolve(Terminator)
    };
  }

  protected override TerminatableAlgorithmInstance<TG, TS, TP, TSearchState> CreateAlgorithmInstance(Run run, ExecutionState executionState)
  {
    return new TerminatableAlgorithmInstance<TG, TS, TP, TSearchState>(
      run,
      executionState.Evaluator,
      executionState.Algorithm,
      executionState.Terminator
    );
  }
}

public class TerminatableAlgorithmInstance<TG, TS, TP, TSearchState> : AlgorithmInstance<TG, TS, TP, TSearchState>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TSearchState : class, ISearchState
{
  protected readonly IAlgorithmInstance<TG, TS, TP, TSearchState> Algorithm;
  protected readonly ITerminatorInstance<TG, TS, TP, TSearchState> Terminator;

  public TerminatableAlgorithmInstance(Run run, IEvaluatorInstance<TG, TS, TP> evaluator, IAlgorithmInstance<TG, TS, TP, TSearchState> algorithm, ITerminatorInstance<TG, TS, TP, TSearchState> terminator)
    : base(run, evaluator)
  {
    Algorithm = algorithm;
    Terminator = terminator;
  }

  public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TP problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
  {
    // ToDo: IMPORTANT: probably we should actually not check the termination condition here, as we advance terminator state on accident
    if (initialState is not null && Terminator.ShouldTerminate(initialState, problem.SearchSpace, problem)) {
      yield break;
    }

    await foreach (var state in Algorithm.RunStreamingAsync(problem, random, initialState, ct)) {
      yield return state;

      if (Terminator.ShouldTerminate(state, problem.SearchSpace, problem)) {
        yield break;
      }
    }
  }
}

public static class TerminatableAlgorithmExtensions
{
  extension<TG, TS, TP, TSearchState>(IAlgorithm<TG, TS, TP, TSearchState> algorithm)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TSearchState : class, ISearchState
  {
    public TerminatableAlgorithm<TG, TS, TP, TSearchState> WithMaxIterations(int maxIterations)
    {
      return new TerminatableAlgorithm<TG, TS, TP, TSearchState> {
        Algorithm = algorithm,
        Terminator = new AfterIterationsTerminator<TG>(maxIterations)
      };
    }
  }
}

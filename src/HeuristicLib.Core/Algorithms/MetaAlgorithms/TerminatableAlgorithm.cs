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
public record TerminatableAlgorithm<TG, TS, TP, TR>
  : Algorithm<TG, TS, TP, TR, TerminatableAlgorithm<TG, TS, TP, TR>.State>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  public required IAlgorithm<TG, TS, TP, TR> Algorithm { get; init; }
  public required ITerminator<TG, TS, TP, TR> Terminator { get; init; }

  protected override State CreateInitialState(ExecutionInstanceRegistry instanceRegistry) => new(instanceRegistry, this);

  protected override async IAsyncEnumerable<TR> RunStreamingAsync(State state, TP problem, IRandomNumberGenerator random, TR? initialState = null, CancellationToken ct = default)
  {
    // ToDo: IMPORTANT: probably we should actually not check the termination condition here, as we advance terminator state on accident
    if (initialState is not null && state.Terminator.ShouldTerminate(initialState, problem.SearchSpace, problem)) {
      yield break;
    }

    await foreach (var algState in Algorithm.RunStreamingAsync(problem, random, initialState, ct)) {
      yield return algState;

      if (state.Terminator.ShouldTerminate(algState, problem.SearchSpace, problem))
        yield break;
    }
  }

  public class State : Algorithm<TG, TS, TP, TR, State>.AlgorithmState
  {
    public readonly IAlgorithmInstance<TG, TS, TP, TR> Algorithm;
    public readonly ITerminatorInstance<TG, TS, TP, TR> Terminator;

    public State(ExecutionInstanceRegistry instanceRegistry, TerminatableAlgorithm<TG, TS, TP, TR> algorithm) : base(instanceRegistry, algorithm)
    {
      Terminator = instanceRegistry.Resolve(algorithm.Terminator);
      Algorithm = instanceRegistry.Resolve(algorithm.Algorithm);
    }
  }
}

public static class TerminatableAlgorithmExtensions
{
  extension<TG, TS, TP, TR>(IAlgorithm<TG, TS, TP, TR> algorithm)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : class, IAlgorithmState
  {
    public TerminatableAlgorithm<TG, TS, TP, TR> WithMaxIterations(int maxIterations)
    {
      return new TerminatableAlgorithm<TG, TS, TP, TR> {
        Algorithm = algorithm,
        Terminator = new AfterIterationsTerminator<TG>(maxIterations)
      };
    }
  }
}

using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public class TerminatableAlgorithm<TG, TS, TP, TR>
  : Algorithm<TG, TS, TP, TR>, ITerminatableAlgorithm<TG, TS, TP, TR>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  public required IAlgorithm<TG, TS, TP, TR> Algorithm { get; init; }
  public required ITerminator<TG, TR, TS, TP> Terminator { get; init; }

  public override async IAsyncEnumerable<TR> RunStreamingAsync(TR? initialState, TP problem, IRandomNumberGenerator random, CancellationToken ct = default)
  {
    // this holds the local state of the terminator
    var shouldTerminate = Terminator.CreateShouldTerminatePredicate(problem.SearchSpace, problem);

    if (initialState is not null && shouldTerminate(initialState)) {
      yield break;
    }
    
    await foreach (var state in Algorithm.RunStreamingAsync(initialState, problem, random, ct)) {
      yield return state;

      if (shouldTerminate(state)) {
        yield break;
      }
    }
  }
}

public static class TerminatableAlgorithmExtensions
{
  extension<TG, TS, TP, TR>(IAlgorithm<TG, TS, TP, TR> algorithm)
    where TG : class
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

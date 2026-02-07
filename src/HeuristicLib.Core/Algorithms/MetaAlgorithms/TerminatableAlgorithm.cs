using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public class TerminatableAlgorithm<TG, TS, TP, TR>
  : Algorithm<TG, TS, TP, TR>, ITerminatableAlgorithm<TG, TS, TP, TR>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  public required IAlgorithm<TG, TS, TP, TR> Algorithm { get; init; }
  public required ITerminator<TG, TR, TS, TP> Terminator { get; init; }


  public override TerminatableAlgorithmInstance<TG, TS, TP, TR> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var evaluatorInstance = instanceRegistry.GetOrCreate(Evaluator);
    var terminatorInstance = instanceRegistry.GetOrCreate(Terminator);
    var algorithmInstance = instanceRegistry.GetOrCreate(Algorithm);
    
    return new TerminatableAlgorithmInstance<TG, TS, TP, TR>(
      evaluatorInstance,
      algorithmInstance,
      terminatorInstance
    );
  }
}

public class TerminatableAlgorithmInstance<TG, TS, TP, TR> : AlgorithmInstance<TG, TS, TP, TR>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  protected readonly IAlgorithmInstance<TG, TS, TP, TR> Algorithm;
  protected readonly ITerminatorInstance<TG, TR, TS, TP> Terminator;

  public TerminatableAlgorithmInstance(IEvaluatorInstance<TG, TS, TP> evaluator, IAlgorithmInstance<TG, TS, TP, TR> algorithm, ITerminatorInstance<TG, TR, TS, TP> terminator) 
    : base(evaluator)
  {
    Algorithm = algorithm;
    Terminator = terminator;
  }

  public override async IAsyncEnumerable<TR> RunStreamingAsync(TP problem, IRandomNumberGenerator random, TR? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
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

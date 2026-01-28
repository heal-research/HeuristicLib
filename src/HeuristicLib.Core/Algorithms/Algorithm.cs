using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Observation;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract class Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{
  public required ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem> Terminator { get; init; }
  public IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? Interceptor { get; init; }
  public IIterationObserver<TGenotype, TSearchSpace, TProblem, TAlgorithmState>? Observer { get; init; }

  public required IEvaluator<TGenotype, TSearchSpace, TProblem> Evaluator { get; init; }

  public virtual IAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecution() => new Execution();
  
  public class Execution : IAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public async virtual IAsyncEnumerable<TAlgorithmState> ExecuteStreamingAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      [EnumeratorCancellation] CancellationToken ct = default
    )
    {
      yield break;
    }
  }
  
  //public abstract IAsyncEnumerable<TAlgorithmState> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken ct = default);
}

public static class AlgorithmExtensions
{
  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TAlgorithmState> algorithm)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
  {
    public async Task<TAlgorithmState> ExecuteAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return await algorithm.ExecuteStreamingAsync(problem, random, initialState, ct).LastAsync(cancellationToken: ct);
    }

    public IEnumerable<TAlgorithmState> ExecuteStreaming(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithm.ExecuteStreamingAsync(problem, random, initialState, ct).ToBlockingEnumerable();
    }

    public TAlgorithmState Execute(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithm.ExecuteAsync(problem, random, initialState, ct: ct).GetAwaiter().GetResult();
    }
  }
  
  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState> algorithm)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
  {
    public IAsyncEnumerable<TAlgorithmState> ExecuteStreamingAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithm.CreateExecution().ExecuteStreamingAsync(problem, random, initialState, ct);
    }
    
    public async Task<TAlgorithmState> ExecuteAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return await algorithm.CreateExecution().ExecuteStreamingAsync(problem, random, initialState, ct).LastAsync(cancellationToken: ct);
    }

    public IEnumerable<TAlgorithmState> ExecuteStreaming(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithm.CreateExecution().ExecuteStreamingAsync(problem, random, initialState, ct).ToBlockingEnumerable();
    }

    public TAlgorithmState Execute(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithm.CreateExecution().ExecuteAsync(problem, random, initialState, ct).GetAwaiter().GetResult();
    }
  }
}

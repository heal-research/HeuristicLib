using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Observation;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Executors;

public class GridExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> 
  : IMetaAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  public required Grid<TAlgorithm> ParameterGrid { get; init; }

  // IMetaAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> IMetaAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>.CreateExecution() => this.CreateExecution();
  // public virtual Execution CreateExecution() => new Execution(this);

  // public class Execution : IMetaAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>
  // {
  //   private readonly GridExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> executor;
  //   
  //   public Execution(GridExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> executor) {
  //     this.executor = executor;
  //   }

  public IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? Interceptor { get; init; }
  public IIterationObserver<TGenotype, TSearchSpace, TProblem, TAlgorithmState>? Observer { get; init; }
  
  public IReadOnlyList<KeyValuePair<TAlgorithm, IAsyncEnumerable<TAlgorithmState>>> RunStreamingAsync(TAlgorithmState? initialState, TProblem problem, IRandomNumberGenerator random, CancellationToken ct = default)
  {
    var algorithms = ParameterGrid.GetConfigurations();
    return algorithms.Select((alg, index) => {
      var algRng = random.Fork(index);
      return KeyValuePair.Create(alg, alg.RunStreamingAsync(initialState, problem, algRng, ct));
    }).ToList();
  }

  async IAsyncEnumerable<TAlgorithmState> IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>.RunStreamingAsync(TAlgorithmState? initialState, TProblem problem, IRandomNumberGenerator random, [EnumeratorCancellation] CancellationToken ct)
  {
    foreach (var stream in RunStreamingAsync(initialState, problem, random, ct)) {
      await foreach (var state in stream.Value.WithCancellation(ct)) {
        yield return state;
      }
    }
  }


  // public IReadOnlyList<(TAlgorithm, IAsyncEnumerable<TAlgorithmState>)> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
  //   {
  //     var algorithms = executor.ParameterGrid.GetConfigurations();
  //     return algorithms.Select((alg, index) => {
  //       var algRng = random.Fork(index);
  //       return (alg, alg.ExecuteStreamingAsync(problem, algRng, initialState, cancellationToken));
  //     }).ToList();
  //   }
  //}
}

public static class GridExecutorExtensions
{
  extension<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IMetaAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> algorithm)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public IReadOnlyList<(TAlgorithm, TAlgorithmState)> RunToCompletion(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      return algorithm.RunStreamingAsync(initialState, problem, random, cancellationToken)
        .Select(alg => (alg.Key, alg.Value.LastAsync(cancellationToken).AsTask().Result))
        .ToList();
    }
    
    // ToDo: add other Run versions, including the default null version.
  }
  
  // extension<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IMetaAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> executor)
  //   where TGenotype : class
  //   where TSearchSpace : class, ISearchSpace<TGenotype>
  //   where TProblem : class, IProblem<TGenotype, TSearchSpace>
  //   where TAlgorithmState : class, IAlgorithmState
  //   where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  // {
  //   public IReadOnlyList<(TAlgorithm, IAsyncEnumerable<TAlgorithmState>)> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
  //   {
  //     return executor.CreateExecution().ExecuteStreamingAsync(problem, random, initialState, cancellationToken);
  //   } 
  //   
  //   public IReadOnlyList<(TAlgorithm, TAlgorithmState)> Execute(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
  //   {
  //     return executor.CreateExecution().Execute(problem, random, initialState, cancellationToken);
  //   }
  // }
}

// public static class GridExecutor
// {
//   public static GridExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> Create<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>(
//     TAlgorithm algorithm, Grid<TAlgorithm> grid
//   )
//     where TGenotype : class
//     where TSearchSpace : class, ISearchSpace<TGenotype>
//     where TProblem : class, IProblem<TGenotype, TSearchSpace>
//     where TAlgorithmState : class, IAlgorithmState
//     where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
//   {
//     return new GridExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> {
//       ParameterGrid = grid
//     };
//   }
// }

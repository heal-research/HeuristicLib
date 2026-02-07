using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm<TGenotype, in TSearchSpace, in TProblem, TAlgorithmState>
  : IExecutable<IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{
  new IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
  
  // IStateTransformerInstance<TAlgorithmState, TGenotype, TSearchSpace, TProblem> IExecutable<IStateTransformerInstance<TAlgorithmState, TGenotype, TSearchSpace, TProblem>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
  //   CreateExecutionInstance(instanceRegistry);
  
  //IIterationObserver<TGenotype, TSearchSpace, TProblem, TAlgorithmState>? Observer { get; }
}

public interface IAlgorithmInstance<TGenotype, in TSearchSpace, in TProblem, TAlgorithmState>
  : IExecutionInstance
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{
  IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(
    TProblem problem,
    IRandomNumberGenerator random,
    TAlgorithmState? initialState = null,
    CancellationToken ct = default
  );
}

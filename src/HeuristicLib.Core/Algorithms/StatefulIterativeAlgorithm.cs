using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record StatefulIterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TRuntimeState>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TSearchState>,
    IIterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, IAlgorithmState
  where TRuntimeState : class
{
  public IInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>? Interceptor { get; init; }

  protected abstract TRuntimeState CreateInitialRuntimeState();

  protected abstract TSearchState ExecuteStep(
    TSearchState? previousState,
    TRuntimeState runtimeState,
    IOperatorExecutor executor,
    TProblem problem,
    IRandomNumberGenerator random);

  public sealed override IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var interceptorInstance = Interceptor is not null ? instanceRegistry.Resolve(Interceptor) : null;
    var evaluatorInstance = instanceRegistry.Resolve(Evaluator);
    var executor = new OperatorExecutor(instanceRegistry);

    return new Instance(
      this,
      instanceRegistry.Run,
      interceptorInstance,
      evaluatorInstance,
      executor,
      CreateInitialRuntimeState());
  }

  private sealed class Instance(
    StatefulIterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TRuntimeState> algorithm,
    Run run,
    IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TSearchState>? interceptor,
    IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator,
    IOperatorExecutor executor,
    TRuntimeState runtimeState)
    : AlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState>(run, evaluator),
      IIterativeAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState>
  {
    private readonly IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TSearchState>? interceptor = interceptor;

    public TSearchState ExecuteStep(TSearchState? previousState, TProblem problem, IRandomNumberGenerator random)
    {
      return algorithm.ExecuteStep(previousState, runtimeState, executor, problem, random);
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TSearchState? initialState = null,
      [EnumeratorCancellation] CancellationToken ct = default)
    {
      var previousState = initialState;

      foreach (var currentIteration in Enumerable.InfiniteSequence(0, 1)) {
        ct.ThrowIfCancellationRequested();
        var iterationRandom = random.Fork(currentIteration);
        var newState = ExecuteStep(previousState, problem, iterationRandom);
        if (interceptor is not null) {
          newState = interceptor.Transform(newState, previousState, problem.SearchSpace, problem);
        }

        yield return newState;

        await Task.Yield();

        previousState = newState;
      }
    }
  }
}

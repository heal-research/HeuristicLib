using System.Runtime.CompilerServices;
using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

// ToDo: maybe we need another base class for MetaAlgorithms like this?
// ToDo: think if we want the CycleAlgorithm to terminate internally by checking each result of the inner algorihtms
[Equatable]
public partial record CycleAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TSearchState>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TSearchState, CycleAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TSearchState>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
  where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
{
  public new sealed class ExecutionState
    : Algorithm<TGenotype, TSearchSpace, TProblem, TSearchState, ExecutionState>.ExecutionState
  {
  }

  [OrderedEquality]
  public ImmutableArray<TAlgorithm> Algorithms { get; }

  // ToDo: think if better place outside and keep CycleAlgorithm as infinite cycles?
  public int? MaximumCycles { get; init; }

  // ToDo: maybe we need a new concept of ExecutionScope for this, if this comes up more often.
  public bool NewExecutionInstancesPerCycle { get; init; } = true;

  public CycleAlgorithm(ImmutableArray<TAlgorithm> algorithms)
  {
    Algorithms = algorithms;
  }

  protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
  {
    return new ExecutionState {
      Evaluator = resolver.Resolve(Evaluator)
    };
  }

  protected override CycleAlgorithmInstance<TAlgorithm, TGenotype, TSearchSpace, TProblem, TSearchState> CreateAlgorithmInstance(Run run, ExecutionState executionState)
  {
    return new CycleAlgorithmInstance<TAlgorithm, TGenotype, TSearchSpace, TProblem, TSearchState>(
      run,
      executionState.Evaluator,
      Algorithms.ToList(),
      MaximumCycles,
      NewExecutionInstancesPerCycle
    );
  }
}

public class CycleAlgorithmInstance<TAlgorithm, TGenotype, TSearchSpace, TProblem, TSearchState>
  : AlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
  where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
{
  protected readonly IReadOnlyList<TAlgorithm> Algorithms;
  protected readonly int? MaximumCycles;
  protected readonly bool NewExecutionInstancesPerCycle;

  private readonly Dictionary<TAlgorithm, ExecutionInstanceRegistry> algorithmInstanceRegistries;

  public CycleAlgorithmInstance(Run run, IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator, IReadOnlyList<TAlgorithm> algorithms, int? maximumCycles, bool newExecutionInstancesPerCycle)
    : base(run, evaluator)
  {
    Algorithms = algorithms;
    MaximumCycles = maximumCycles;
    NewExecutionInstancesPerCycle = newExecutionInstancesPerCycle;

    algorithmInstanceRegistries = new Dictionary<TAlgorithm, ExecutionInstanceRegistry>(capacity: NewExecutionInstancesPerCycle ? Algorithms.Count : 0);
  }

  public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
  {
    var state = initialState;

    var cycleCountGenerator = MaximumCycles.HasValue
      ? Enumerable.Range(0, MaximumCycles.Value)
      : Enumerable.InfiniteSequence(0, 1);

    foreach (var cycleCount in cycleCountGenerator) {
      var cycleRng = random.Fork(cycleCount);
      foreach (var (algorithm, algorithmIndex) in Algorithms.Select((a, i) => (a, i))) {
        var algorithmRng = cycleRng.Fork(algorithmIndex);
        var registry = ExecutionInstanceRegistry(algorithm, Run);
        var algorithmInstance = algorithm.CreateExecutionInstance(registry);

        await foreach (var newState in algorithmInstance.RunStreamingAsync(problem, algorithmRng, state, ct)) {
          state = newState;
          yield return newState;
        }
      }
    }
  }

  private ExecutionInstanceRegistry ExecutionInstanceRegistry(TAlgorithm algorithm, Run run)
  {
    if (NewExecutionInstancesPerCycle) {
      return run.CreateNewRegistry();
    }

    if (algorithmInstanceRegistries.TryGetValue(algorithm, out var existingRegistry)) {
      return existingRegistry;
    }

    var newRegistry = run.CreateChildRegistry();
    algorithmInstanceRegistries[algorithm] = newRegistry;
    return newRegistry;
  }
}

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
public partial record CycleAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, CycleAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>.State>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
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

  protected override State CreateInitialState(ExecutionInstanceRegistry instanceRegistry) => new(instanceRegistry, this);

  protected override async IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(State cycleState, TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
  {
    var state = initialState;

    var cycleCountGenerator = MaximumCycles.HasValue
      ? Enumerable.Range(0, MaximumCycles.Value)
      : Enumerable.InfiniteSequence(0, 1);

    foreach (var cycleCount in cycleCountGenerator) {
      var cycleRng = random.Fork(cycleCount);
      foreach (var (algorithm, algorithmIndex) in Algorithms.Select((a, i) => (a, i))) {
        var algorithmRng = cycleRng.Fork(algorithmIndex);
        var registry = ExecutionInstanceRegistry(cycleState, algorithm);
        var algorithmInstance = algorithm.CreateExecutionInstance(registry);

        await foreach (var newState in algorithmInstance.RunStreamingAsync(problem, algorithmRng, state, ct)) {
          state = newState;
          yield return newState;
        }

        if (ct.IsCancellationRequested) yield break;
      }
    }
  }

  private ExecutionInstanceRegistry ExecutionInstanceRegistry(State cycleState, TAlgorithm algorithm)
  {
    if (NewExecutionInstancesPerCycle)
      return cycleState.Run.CreateNewRegistry();

    if (cycleState.AlgorithmInstanceRegistries.TryGetValue(algorithm, out var existingRegistry))
      return existingRegistry;

    var newRegistry = cycleState.Run.CreateChildRegistry();
    cycleState.AlgorithmInstanceRegistries[algorithm] = newRegistry;
    return newRegistry;
  }

  public class State : AlgorithmState
  {
    public Dictionary<TAlgorithm, ExecutionInstanceRegistry> AlgorithmInstanceRegistries { get; }

    public State(ExecutionInstanceRegistry instanceRegistry, CycleAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState> algorithm) : base(instanceRegistry, algorithm)
    {
      AlgorithmInstanceRegistries = new Dictionary<TAlgorithm, ExecutionInstanceRegistry>(capacity: algorithm.NewExecutionInstancesPerCycle ? algorithm.Algorithms.Length : 0);
    }
  }
}

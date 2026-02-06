using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

// ToDo: maybe we need another base class for MetaAlgorithms like this?
// ToDo: think if we want the CycleAlgorithm to terminate internally by checking each result of the inner algorihtms
public class CycleAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  public ImmutableList<TAlgorithm> Algorithms { get; }
  
  // ToDo: think if better place outside and keep CycleAlgorithm as infinite cycles?
  public int? MaximumCycles { get; init; }
  
  // ToDo: maybe we need a new concept of ExecutionScope for this, if this comes up more often.
  public bool NewExecutionInstancesPerCycle { get; init; } = true;

  public CycleAlgorithm(IReadOnlyList<TAlgorithm> algorithms)
  {
    Algorithms = new ImmutableList<TAlgorithm>(algorithms);
  }

  public override CycleAlgorithmInstance<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var evaluatorInstance = Evaluator.CreateExecutionInstance(instanceRegistry);
    
    return new CycleAlgorithmInstance<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>(
      evaluatorInstance,
      Algorithms.ToList(),
      MaximumCycles,
      NewExecutionInstancesPerCycle
    );
  }
}

public class CycleAlgorithmInstance<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : AlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  protected readonly IReadOnlyList<TAlgorithm> Algorithms;
  protected readonly int? MaximumCycles;
  protected readonly bool NewExecutionInstancesPerCycle;

  private readonly Dictionary<TAlgorithm, ExecutionInstanceRegistry> algorithmInstanceRegistries;
  
  public CycleAlgorithmInstance(IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator, IReadOnlyList<TAlgorithm> algorithms, int? maximumCycles, bool newExecutionInstancesPerCycle) 
    : base(evaluator)
  {
    Algorithms = algorithms;
    MaximumCycles = maximumCycles;
    NewExecutionInstancesPerCycle = newExecutionInstancesPerCycle;
    
    algorithmInstanceRegistries = new Dictionary<TAlgorithm, ExecutionInstanceRegistry>(capacity: NewExecutionInstancesPerCycle ? Algorithms.Count : 0);
  }

  public override async IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
  {
    var state = initialState;

    var cycleCountGenerator = MaximumCycles.HasValue 
      ? Enumerable.Range(0, MaximumCycles.Value) 
      : Enumerable.InfiniteSequence(0, 1);
      
    foreach (var cycleCount in cycleCountGenerator) {
      var cycleRng = random.Fork(cycleCount);
      foreach (var (algorithm, algorithmIndex) in Algorithms.Select((a, i) => (a, i))) {
        var algorithmRng = cycleRng.Fork(algorithmIndex);
        var registry = ExecutionInstanceRegistry(algorithm);
        var algorithmInstance = algorithm.CreateExecutionInstance(registry);
        
        await foreach (var newState in algorithmInstance.RunStreamingAsync(problem, algorithmRng, state, ct)) {
          state = newState;
          yield return newState;
        }
      }
    }
  }
  
  private ExecutionInstanceRegistry ExecutionInstanceRegistry(TAlgorithm algorithm)
  {
    if (NewExecutionInstancesPerCycle) {
      return new ExecutionInstanceRegistry();
    }
    
    if (algorithmInstanceRegistries.TryGetValue(algorithm, out var existingRegistry)) {
      return existingRegistry;
    }
    
    var newRegistry = new ExecutionInstanceRegistry();
    algorithmInstanceRegistries[algorithm] = newRegistry;
    return newRegistry;
  }
}

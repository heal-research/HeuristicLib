using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Collections;
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

  public CycleAlgorithm(IReadOnlyList<TAlgorithm> algorithms)
  {
    Algorithms = new ImmutableList<TAlgorithm>(algorithms);
  }

  public override async IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState, [EnumeratorCancellation] CancellationToken ct = default)
  {
    var state = initialState;

    var cycleCountGenerator = MaximumCycles.HasValue 
      ? Enumerable.Range(0, MaximumCycles.Value) 
      : Enumerable.InfiniteSequence(0, 1);
      
    foreach (var cycleCount in cycleCountGenerator) {
      var cycleRng = random.Fork(cycleCount);
      foreach (var (algorithm, algorithmIndex) in Algorithms.Select((a, i) => (a, i))) {
        var algorithmRng = cycleRng.Fork(algorithmIndex);
        await foreach (var newState in algorithm.RunStreamingAsync(problem, algorithmRng, state, ct)) {
          state = newState;
          yield return newState;
        }
      }
    }
  }
}

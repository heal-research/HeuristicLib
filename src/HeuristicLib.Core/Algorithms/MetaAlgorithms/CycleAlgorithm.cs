using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public class CycleAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  public ImmutableList<TAlgorithm> Algorithms { get;}
  
  public CycleAlgorithm(IReadOnlyList<TAlgorithm> algorithms)
  {
    Algorithms = new ImmutableList<TAlgorithm>(algorithms);
  }

  public override async IAsyncEnumerable<TAlgorithmState> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default) {
    var state = initialState;

    foreach (int cycle in Enumerable.InfiniteSequence(0, 1)) {
      var cycleRng = random.Fork(cycle);
      foreach (var (algorithm, algorithmIndex) in Algorithms.Select((a, i) => (a, i))) {
        var algorithmRng = cycleRng.Fork(algorithmIndex);
        await foreach (var newState in algorithm.ExecuteStreamingAsync(problem, algorithmRng, state, ct)) {
          state = newState;
          yield return newState;
        }
      }
    }
  }
}

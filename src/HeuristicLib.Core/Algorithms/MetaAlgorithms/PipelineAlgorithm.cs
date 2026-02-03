using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

// ToDo: Add Pipeline with different State types
// ToDo: Add support for Transformation between different (or the same typed) states.

public class PipelineAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  public ImmutableList<TAlgorithm> Algorithms { get; }

  public PipelineAlgorithm(IReadOnlyList<TAlgorithm> algorithms)
  {
    Algorithms = new ImmutableList<TAlgorithm>(algorithms);
  }

  public override async IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(TAlgorithmState? initialState, TProblem problem, IRandomNumberGenerator random, [EnumeratorCancellation] CancellationToken ct = default)
  {
    var state = initialState;

    foreach (var (algorithm, index) in Algorithms.Select((a, i) => (Algorithm: a, Index: i))) {
      var algRng = random.Fork(index);
      await foreach (var newState in algorithm.RunStreamingAsync(state, problem, algRng, ct)) {
        state = newState;
        yield return newState;
      }
    }
  }
}

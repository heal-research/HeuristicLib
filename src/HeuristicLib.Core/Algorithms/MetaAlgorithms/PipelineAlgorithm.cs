using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

// ToDo: Add Pipeline with different State types
// ToDo: Add support for Transformation between different (or the same typed) states.

[Equatable]
public partial record PipelineAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, NoState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  [OrderedEquality]
  public ImmutableArray<TAlgorithm> Algorithms { get; }

  public PipelineAlgorithm(ImmutableArray<TAlgorithm> algorithms)
  {
    Algorithms = algorithms;
  }

  protected override NoState CreateInitialState(ExecutionInstanceRegistry instanceRegistry) => NoState.Instance;

  protected override async IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(NoState _, TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken ct = default)
  {
    var state = initialState;
    foreach (var (algorithm, index) in Algorithms.Select((a, i) => (a, i))) {
      var algRng = random.Fork(index);
      // run a new fresh stream here
      await foreach (var newState in algorithm.RunStreamingAsync(problem, algRng, state, ct)) {
        state = newState;
        yield return newState;
      }
    }
  }
}

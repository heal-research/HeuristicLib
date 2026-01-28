using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

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

  public override Execution CreateExecution() => new(this);

  public class Execution : IAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    private readonly PipelineAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState> pipeline;

    public Execution(PipelineAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState> pipeline)
    {
      this.pipeline = pipeline;
    }

    public virtual async IAsyncEnumerable<TAlgorithmState> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
      var state = initialState;

      foreach (var (algorithm, index) in pipeline.Algorithms.Select((a, i) => (Algorithm: a, Index: i))) {
        var algRng = random.Fork(index);
        var algExecution = algorithm.CreateExecution();
        await foreach (var newState in algorithm.ExecuteStreamingAsync(problem, algRng, state, ct)) {
          state = newState;
          yield return newState;
        }
      }
    }
  }
}

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
  public ImmutableList<TAlgorithm> Algorithms { get; }
  
  public int? MaximumCycles { get; init; }

  public CycleAlgorithm(IReadOnlyList<TAlgorithm> algorithms)
  {
    Algorithms = new ImmutableList<TAlgorithm>(algorithms);
  }
  
  public override Execution CreateExecution() => new(this);

  public class Execution : IAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    private readonly CycleAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState> cycle;

    public Execution(CycleAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState> cycle)
    {
      this.cycle = cycle;
    }

    public virtual async IAsyncEnumerable<TAlgorithmState> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
      var state = initialState;

      var cycleCountGenerator = cycle.MaximumCycles.HasValue 
        ? Enumerable.Range(0, cycle.MaximumCycles.Value) 
        : Enumerable.InfiniteSequence(0, 1);
      
      foreach (var cycleCount in cycleCountGenerator) {
        var cycleRng = random.Fork(cycleCount);
        foreach (var (algorithm, algorithmIndex) in cycle.Algorithms.Select((a, i) => (a, i))) {
          var algorithmRng = cycleRng.Fork(algorithmIndex);
          var algorithmExecution = algorithm.CreateExecution();
          await foreach (var newState in algorithmExecution.ExecuteStreamingAsync(problem, algorithmRng, state, ct)) {
            state = newState;
            yield return newState;
          }
        }
      }
    }
  }
}

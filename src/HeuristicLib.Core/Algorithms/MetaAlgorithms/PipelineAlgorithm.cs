using System.Runtime.CompilerServices;
using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

// ToDo: Add Pipeline with different State types
// ToDo: Add support for Transformation between different (or the same typed) states.

[Equatable]
public partial record PipelineAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  [OrderedEquality] public ImmutableArray<TAlgorithm> Algorithms { get; }

  public PipelineAlgorithm(ImmutableArray<TAlgorithm> algorithms)
  {
    Algorithms = algorithms;
  }

  public override PipelineAlgorithmInstance<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var evaluatorInstance = instanceRegistry.GetOrCreate(Evaluator);

    return new PipelineAlgorithmInstance<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>(
      evaluatorInstance,
      Algorithms
    );
  }
}

public class PipelineAlgorithmInstance<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : AlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  protected readonly IReadOnlyList<TAlgorithm> Algorithms;

  public PipelineAlgorithmInstance(IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator, IReadOnlyList<TAlgorithm> algorithms)
    : base(evaluator)
  {
    Algorithms = algorithms;
  }

  public PipelineAlgorithmInstance(
    IReadOnlyList<TAlgorithm> algorithms,
    IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator)
    : base(evaluator)
  {
    Algorithms = algorithms;
  }

  public override async IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
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

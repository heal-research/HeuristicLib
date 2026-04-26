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
public partial record PipelineAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TSearchState>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TSearchState, PipelineAlgorithm<TAlgorithm, TGenotype, TSearchSpace, TProblem, TSearchState>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
  where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
{
  public new sealed class ExecutionState
    : Algorithm<TGenotype, TSearchSpace, TProblem, TSearchState, ExecutionState>.ExecutionState
  {
  }

  [OrderedEquality] public ImmutableArray<TAlgorithm> Algorithms { get; }

  public PipelineAlgorithm(ImmutableArray<TAlgorithm> algorithms)
  {
    Algorithms = algorithms;
  }

  protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
  {
    return new ExecutionState {
      Evaluator = resolver.Resolve(Evaluator)
    };
  }

  protected override PipelineAlgorithmInstance<TAlgorithm, TGenotype, TSearchSpace, TProblem, TSearchState> CreateAlgorithmInstance(Run run, ExecutionState executionState)
  {
    return new PipelineAlgorithmInstance<TAlgorithm, TGenotype, TSearchSpace, TProblem, TSearchState>(
      run,
      executionState.Evaluator,
      Algorithms
    );
  }
}

public class PipelineAlgorithmInstance<TAlgorithm, TGenotype, TSearchSpace, TProblem, TSearchState>
  : AlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
  where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
{
  protected readonly IReadOnlyList<TAlgorithm> Algorithms;

  public PipelineAlgorithmInstance(Run run, IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator, IReadOnlyList<TAlgorithm> algorithms)
    : base(run, evaluator)
  {
    Algorithms = algorithms;
  }

  public PipelineAlgorithmInstance(
    Run run,
    IReadOnlyList<TAlgorithm> algorithms,
    IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator)
    : base(run, evaluator)
  {
    Algorithms = algorithms;
  }

  public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
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

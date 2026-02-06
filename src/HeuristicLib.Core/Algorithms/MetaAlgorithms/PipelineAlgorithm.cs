using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Evaluators;
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

  public override PipelineAlgorithmInstance<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var evaluatorInstance = instanceRegistry.GetOrAdd(Evaluator, () => Evaluator.CreateExecutionInstance(instanceRegistry));
    
    return new PipelineAlgorithmInstance<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>(
      evaluatorInstance,
      Algorithms
    );
  }
}

public class PipelineAlgorithmInstance<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : AlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  protected readonly ImmutableList<TAlgorithm> Algorithms;

  public PipelineAlgorithmInstance(IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator, ImmutableList<TAlgorithm> algorithms) 
    : base(evaluator)
  {
    Algorithms = algorithms;
  }

  public PipelineAlgorithmInstance(
    ImmutableList<TAlgorithm> algorithms,
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

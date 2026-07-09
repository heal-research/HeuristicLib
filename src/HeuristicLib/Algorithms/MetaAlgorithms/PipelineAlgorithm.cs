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
public partial record PipelineAlgorithm<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>
  : Algorithm<TCandidate, TSearchSpace, TProblem, TSearchState, PipelineAlgorithm<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
  where TAlgorithm : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    public new sealed class ExecutionState
      : Algorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ExecutionState>.ExecutionState
    {
    }

    [OrderedEquality] public ImmutableArray<TAlgorithm> Algorithms { get; }

    public PipelineAlgorithm(ImmutableArray<TAlgorithm> algorithms)
    {
        Algorithms = algorithms;
    }

    protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
    {
        return new ExecutionState
        {
            Evaluator = resolver.Resolve(Evaluator)
        };
    }

    protected override PipelineAlgorithmInstance<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState> CreateAlgorithmInstance(Run run, ExecutionState executionState)
    {
        return new PipelineAlgorithmInstance<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>(
          run,
          executionState.Evaluator,
          Algorithms
        );
    }
}

public class PipelineAlgorithmInstance<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>
  : AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
  where TAlgorithm : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    protected readonly IReadOnlyList<TAlgorithm> Algorithms;

    public PipelineAlgorithmInstance(Run run, IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator, IReadOnlyList<TAlgorithm> algorithms)
      : base(run, evaluator)
    {
        Algorithms = algorithms;
    }

    public PipelineAlgorithmInstance(
      Run run,
      IReadOnlyList<TAlgorithm> algorithms,
      IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator)
      : base(run, evaluator)
    {
        Algorithms = algorithms;
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var state = initialState;

        foreach (var (algorithm, index) in Algorithms.Select((a, i) => (a, i)))
        {
            var algRng = random.Fork(index);
            var registry = Run.CreateNewRegistry();
            var algorithmInstance = algorithm.CreateExecutionInstance(registry);

            await foreach (var newState in algorithmInstance.RunStreamingAsync(problem, algRng, state, ct))
            {
                state = newState;
                yield return newState;
            }
        }
    }
}

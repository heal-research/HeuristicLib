using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

// Adapter for algorithms that do not have an inner termination criterion; revisit if every algorithm exposes a terminal-state hook.
public record StateTerminatedAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
  : Algorithm<TCandidate, TSearchSpace, TProblem, TSearchState, StateTerminatedAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    public new sealed class ExecutionState
      : Algorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ExecutionState>.ExecutionState
    {
        public required IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm { get; init; }
        public required ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Terminator { get; init; }
    }

    public required IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm { get; init; }
    public required ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> Terminator { get; init; }

    protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
    {
        // Resolve the terminator before the wrapped algorithm so elapsed-time terminators start at the earliest point this wrapper controls, including wrapped algorithm instancing.
        var terminator = resolver.Resolve(Terminator);
        return new ExecutionState
        {
            Evaluator = resolver.Resolve(Evaluator),
            Algorithm = resolver.Resolve(Algorithm),
            Terminator = terminator
        };
    }

    protected override StateTerminatedAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateAlgorithmInstance(Run run, ExecutionState executionState)
    {
        return new StateTerminatedAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(
          run,
          executionState.Evaluator,
          executionState.Algorithm,
          executionState.Terminator
        );
    }
}

public class StateTerminatedAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> : AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    protected readonly IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm;
    protected readonly ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Terminator;

    public StateTerminatedAlgorithmInstance(Run run, IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator, IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> terminator)
      : base(run, evaluator)
    {
        Algorithm = algorithm;
        Terminator = terminator;
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var state in Algorithm.RunStreamingAsync(problem, random, initialState, ct))
        {
            yield return state;

            if (Terminator.IsTerminalState(state, problem.SearchSpace, problem))
            {
                yield break;
            }
        }
    }
}

public static class StateTerminatedAlgorithmExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
      where TSearchSpace : class, ISearchSpace<TCandidate>
      where TProblem : class, IProblem<TCandidate, TSearchSpace>
      where TSearchState : class, ISearchState
    {
        public StateTerminatedAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> WithMaxIterations(int maximumIterations)
        {
            return new StateTerminatedAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
            {
                Algorithm = algorithm,
                Terminator = new AfterIterationsTerminator<TCandidate>(maximumIterations)
            };
        }
    }
}

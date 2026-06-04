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
public record StateTerminatedAlgorithm<TG, TS, TP, TSearchState>
  : Algorithm<TG, TS, TP, TSearchState, StateTerminatedAlgorithm<TG, TS, TP, TSearchState>.ExecutionState>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TSearchState : class, ISearchState
{
    public new sealed class ExecutionState
      : Algorithm<TG, TS, TP, TSearchState, ExecutionState>.ExecutionState
    {
        public required IAlgorithmInstance<TG, TS, TP, TSearchState> Algorithm { get; init; }
        public required ITerminatorInstance<TG, TS, TP, TSearchState> Terminator { get; init; }
    }

    public required IAlgorithm<TG, TS, TP, TSearchState> Algorithm { get; init; }
    public required ITerminator<TG, TS, TP, TSearchState> Terminator { get; init; }

    protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
    {
        return new ExecutionState
        {
            Evaluator = resolver.Resolve(Evaluator),
            Algorithm = resolver.Resolve(Algorithm),
            Terminator = resolver.Resolve(Terminator)
        };
    }

    protected override StateTerminatedAlgorithmInstance<TG, TS, TP, TSearchState> CreateAlgorithmInstance(Run run, ExecutionState executionState)
    {
        return new StateTerminatedAlgorithmInstance<TG, TS, TP, TSearchState>(
          run,
          executionState.Evaluator,
          executionState.Algorithm,
          executionState.Terminator
        );
    }
}

public class StateTerminatedAlgorithmInstance<TG, TS, TP, TSearchState> : AlgorithmInstance<TG, TS, TP, TSearchState>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TSearchState : class, ISearchState
{
    protected readonly IAlgorithmInstance<TG, TS, TP, TSearchState> Algorithm;
    protected readonly ITerminatorInstance<TG, TS, TP, TSearchState> Terminator;

    public StateTerminatedAlgorithmInstance(Run run, IEvaluatorInstance<TG, TS, TP> evaluator, IAlgorithmInstance<TG, TS, TP, TSearchState> algorithm, ITerminatorInstance<TG, TS, TP, TSearchState> terminator)
      : base(run, evaluator)
    {
        Algorithm = algorithm;
        Terminator = terminator;
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TP problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
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
    extension<TG, TS, TP, TSearchState>(IAlgorithm<TG, TS, TP, TSearchState> algorithm)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      where TSearchState : class, ISearchState
    {
        public StateTerminatedAlgorithm<TG, TS, TP, TSearchState> WithMaxIterations(int maxIterations)
        {
            return new StateTerminatedAlgorithm<TG, TS, TP, TSearchState>
            {
                Algorithm = algorithm,
                Terminator = new AfterIterationsTerminator<TG>(maxIterations)
            };
        }
    }
}

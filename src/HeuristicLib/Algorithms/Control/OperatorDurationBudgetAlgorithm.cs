using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, TOperator>
    : Algorithm<OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, TOperator>, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
    where TOperator : class, IOperator
{
    public required IAlgorithm<TCandidate, TSearchState> Algorithm { get; init; }
    public required TOperator ObservedOperator { get; init; }

    public override bool Fits(ExecutionSignature execution) => base.Fits(execution) && execution.Fits(Algorithm, ObservedOperator);

    /// <summary>
    /// Gets the factory that wraps the observed operator in a duration-measuring operator.
    /// </summary>
    /// <remarks>
    /// The factory receives the original observed operator configuration. The returned wrapper resolves that source
    /// through its scope to obtain the already-built inner chain, so enclosing budgets and observations compose.
    /// The returned operator must retain the observed operator's role.
    /// </remarks>
    public required Func<TOperator, DurationAccumulator, TimeProvider, TOperator> MeasuredOperatorFactory { get; init; }
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>
    /// Gets the measured-operator duration budget. The expected value is positive.
    /// </summary>
    /// <remarks>The budget is checked after each produced state, so a nonpositive budget stops after the first state.</remarks>
    public TimeSpan MaximumDuration { get; init; }

    public override ExecutionFactory<IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
    {
        var duration = new DurationAccumulator();
        var childKey = new object();
        return scope =>
        {
            var childScope = scope.GetOrCreateChildScope(childKey, child =>
                child.Wrap(ObservedOperator, current => MeasuredOperatorFactory(current, duration, TimeProvider)));
            return new OperatorDurationBudgetAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(childScope.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(Algorithm), duration, MaximumDuration);
        };
    }
}

public sealed class OperatorDurationBudgetAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm;
    private readonly DurationAccumulator duration;
    private readonly TimeSpan maximumDuration;

    public OperatorDurationBudgetAlgorithmExecution(IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, DurationAccumulator duration, TimeSpan maximumDuration)
    {
        this.algorithm = algorithm;
        this.duration = duration;
        this.maximumDuration = maximumDuration;
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var state in algorithm.RunStreamingAsync(problem, random, initialState, ct))
        {
            yield return state;

            if (duration.CurrentDuration >= maximumDuration)
            {
                yield break;
            }
        }
    }
}

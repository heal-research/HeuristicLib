using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TOperator>
    : Algorithm<OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TOperator>, TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TOperator : class, IOperator, IExecutionInstanceResolvable<IExecutionInstance>
{
    public required IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm { get; init; }
    public required TOperator ObservedOperator { get; init; }

    /// <summary>
    /// Gets the factory that wraps the observed operator in a duration-measuring operator.
    /// </summary>
    /// <remarks>
    /// The factory receives whatever the surrounding execution already resolves for the observed operator, which may
    /// itself be a wrapper installed by an analyzer or by an enclosing budget, so that the decorations compose. The
    /// returned operator is expected to keep the observed operator's role, as every operator wrapper does.
    /// </remarks>
    public required Func<TOperator, ObservationDuration, TimeProvider, TOperator> MeasuredOperatorFactory { get; init; }
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>
    /// Gets the measured-operator duration budget. The expected value is positive.
    /// </summary>
    /// <remarks>The budget is checked after each produced state, so a nonpositive budget stops after the first state.</remarks>
    public TimeSpan MaximumDuration { get; init; }

    public override OperatorDurationBudgetAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    {
        var duration = new ObservationDuration();
        var childRegistry = instanceRegistry.CreateChildRegistry();
        childRegistry.Decorate(ObservedOperator, current => MeasuredOperatorFactory(current, duration, TimeProvider));

        return new(childRegistry.Resolve(Algorithm), duration, MaximumDuration);
    }
}

public sealed class OperatorDurationBudgetAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm;
    private readonly ObservationDuration duration;
    private readonly TimeSpan maximumDuration;

    public OperatorDurationBudgetAlgorithmInstance(IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, ObservationDuration duration, TimeSpan maximumDuration)
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

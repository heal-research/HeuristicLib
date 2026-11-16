using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>Stops its algorithm once a duration reaches a maximum, checked after each produced state.</summary>
/// <remarks>
/// The duration restarts each time this budget is built, such as in each Cycle turn, because the budget shares it in its
/// own scope. The wrappers measuring into it live in <see cref="Algorithm"/>, where <c>LimitedToDuration</c> places them.
/// They add the time of every call, including failed, nested and concurrent calls, so the duration is a sum of call times
/// rather than wall clock time. A copy that replaces <see cref="Algorithm"/> loses those wrappers, and the budget then
/// never stops.
/// </remarks>
public sealed record OperatorDurationBudgetAlgorithm<TCandidate, TSearchState>
    : Algorithm<OperatorDurationBudgetAlgorithm<TCandidate, TSearchState>, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public OperatorDurationBudgetAlgorithm(IAlgorithm<TCandidate, TSearchState> algorithm, Duration duration, TimeSpan maximumDuration)
    {
        Algorithm = algorithm;
        Duration = duration;
        MaximumDuration = maximumDuration;
    }

    public IAlgorithm<TCandidate, TSearchState> Algorithm { get; init; }

    public Duration Duration { get; init; }

    /// <summary>
    /// Gets the measured duration budget. The expected value is positive.
    /// </summary>
    /// <remarks>The budget is checked after each produced state, so a nonpositive budget stops after the first state.</remarks>
    public TimeSpan MaximumDuration { get; init; }

    public static Parameter<OperatorDurationBudgetAlgorithm<TCandidate, TSearchState>, IAlgorithm<TCandidate, TSearchState>> AlgorithmParameter { get; } =
        new(nameof(Algorithm), static budget => budget.Algorithm, static (budget, value) => budget with { Algorithm = value });

    public static Parameter<OperatorDurationBudgetAlgorithm<TCandidate, TSearchState>, Duration> DurationParameter { get; } =
        new(nameof(Duration), static budget => budget.Duration, static (budget, value) => budget with { Duration = value });

    public static Parameter<OperatorDurationBudgetAlgorithm<TCandidate, TSearchState>, TimeSpan> MaximumDurationParameter { get; } =
        new(nameof(MaximumDuration), static budget => budget.MaximumDuration, static (budget, value) => budget with { MaximumDuration = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [AlgorithmParameter, DurationParameter, MaximumDurationParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IAlgorithmExecutor<TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        scope.Share(Duration);
        var algorithm = scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(Algorithm, scope.Problem);
        return new OperatorDurationBudgetAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, scope.Resolve(Duration), MaximumDuration, scope.Problem);
    }
}

public sealed class OperatorDurationBudgetAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithmExecutor<TSearchState> algorithm;
    private readonly DurationAccumulator duration;
    private readonly TimeSpan maximumDuration;

    public OperatorDurationBudgetAlgorithmExecutor(IAlgorithmExecutor<TSearchState> algorithm, DurationAccumulator duration, TimeSpan maximumDuration, TProblem problem)
        : base(problem)
    {
        this.algorithm = algorithm;
        this.duration = duration;
        this.maximumDuration = maximumDuration;
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var state in algorithm.RunStreamingAsync(random, initialState, ct))
        {
            yield return state;

            if (duration.CurrentDuration >= maximumDuration)
            {
                yield break;
            }
        }
    }
}

public static class OperatorDurationBudgetExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>Stops the algorithm once the operator's calls, including failed ones, have taken the given time.</summary>
        /// <remarks>
        /// The operator is matched by reference, and it is an error when the algorithm does not hold it. The operation is
        /// inferred from the operator; for an operator implementing several operations, pass it through a variable of the
        /// operation's operator type.
        /// </remarks>
        /// <param name="observedOperator">The operator whose calls are measured.</param>
        /// <param name="maximumDuration">The time after which the budget stops, checked after each produced state.</param>
        /// <param name="timeProvider">Measures each call. When omitted, the system clock does.</param>
        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState> LimitedToDuration<TOperation>(
            IOperator<TOperation, TCandidate> observedOperator, TimeSpan maximumDuration, TimeProvider? timeProvider = null)
            where TOperation : IOperation<TOperation> =>
            new(algorithm.Timing(observedOperator, out var duration, timeProvider), duration, maximumDuration);
    }

    extension<TAlgorithm>(TAlgorithm algorithm) where TAlgorithm : class, IConfigurationNode
    {
        /// <summary>
        /// Stops the algorithm once the time that <paramref name="placement"/> measures reaches the given duration, such as
        /// <c>ga.LimitedToDuration(TimeSpan.FromSeconds(10), (g, time) => g.Timing(g.Mutator, time).Timing(g.Crossover, time))</c>.
        /// </summary>
        /// <remarks>
        /// The placement runs during this call and is not stored. Timing a target twice adds every call twice. The duration
        /// restarts with the budget and stays inside it; a placement that ignores it leaves a budget that never stops.
        /// </remarks>
        /// <param name="maximumDuration">The time after which the budget stops, checked after each produced state.</param>
        /// <param name="placement">Places the timing wrappers on the algorithm it receives, which is this one, and returns the algorithm to limit.</param>
        /// <param name="timeProvider">Measures each call. When omitted, the system clock does.</param>
        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState> LimitedToDuration<TCandidate, TSearchState>(TimeSpan maximumDuration,
            Func<TAlgorithm, Duration, IAlgorithm<TCandidate, TSearchState>> placement, TimeProvider? timeProvider = null)
            where TSearchState : class, ISearchState
        {
            var duration = new Duration(timeProvider);
            return new(placement(algorithm, duration), duration, maximumDuration);
        }
    }
}

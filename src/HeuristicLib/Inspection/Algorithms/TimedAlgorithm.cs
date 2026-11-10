using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Adds the time each call of its child algorithm spends advancing to its next state, including failures, to a duration.</summary>
/// <remarks>
/// The time between two states belongs to whoever consumes them, so pauses and the handling of a streamed state are not
/// measured. A terminator reading the same duration sees this time wherever the wrapper is placed.
/// </remarks>
public sealed record TimedAlgorithm<TCandidate, TSearchState> : WrappingAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public TimedAlgorithm(IAlgorithm<TCandidate, TSearchState> childAlgorithm, Duration duration)
        : base(childAlgorithm)
    {
        Duration = duration;
    }

    public Duration Duration { get; init; }

    public static Parameter<TimedAlgorithm<TCandidate, TSearchState>, Duration> DurationParameter { get; } =
        new(nameof(Duration), static timed => timed.Duration, static (timed, value) => timed with { Duration = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [DurationParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        new Executor<TSearchSpace, TProblem>(scope.Resolve(ChildAlgorithm), scope.Resolve(Duration), Duration.TimeProvider);

    private sealed class Executor<TSearchSpace, TProblem>(IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm,
        DurationAccumulator duration, TimeProvider timeProvider)
        : WrappingAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(childAlgorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
            TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await using var iterator = ChildAlgorithm.RunStreamingAsync(problem, random, initialState, ct).GetAsyncEnumerator(ct);
            while (true)
            {
                bool moved;
                var startedAt = timeProvider.GetTimestamp();
                try
                {
                    moved = await iterator.MoveNextAsync();
                }
                finally
                {
                    duration.AddDuration(timeProvider.GetElapsedTime(startedAt));
                }
                if (!moved)
                    yield break;
                yield return iterator.Current;
            }
        }
    }
}

public static class TimedAlgorithmExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>Adds the time this algorithm spends advancing to each state, including failures, to the duration.</summary>
        public TimedAlgorithm<TCandidate, TSearchState> Timed(Duration duration) => new(algorithm, duration);

        /// <summary>Measures an algorithm's advances wherever this configuration holds it, such as <c>ga.Timing(ga.Iterations, out var time)</c>.</summary>
        /// <param name="target">The iterations to measure. Their algorithm is matched by reference, and it is an error when this configuration does not hold it.</param>
        /// <param name="duration">Receives the new duration, which <c>run.Result(duration).CurrentDuration</c> reads and a terminator can check.</param>
        /// <param name="timeProvider">Measures each advance. When omitted, the system clock does.</param>
        /// <returns>The interface, because the target may be this algorithm itself, which the wrapper then replaces.</returns>
        public IAlgorithm<TCandidate, TSearchState> Timing(AlgorithmSource<TCandidate, TSearchState, AlgorithmObservation<TCandidate, TSearchState>> target,
            out Duration duration, TimeProvider? timeProvider = null) =>
            algorithm.Substitute(Substitution.Timing(target, out duration, timeProvider));
    }

    extension(Substitution)
    {
        /// <summary>The substitution behind <c>Timing</c>, for one call with others.</summary>
        /// <param name="target">The iterations to measure. Their algorithm is matched by reference, and it is an error when the configuration does not hold it.</param>
        /// <param name="duration">Receives the new duration.</param>
        /// <param name="timeProvider">Measures each advance. When omitted, the system clock does.</param>
        public static Substitution Timing<TCandidate, TSearchState>(AlgorithmSource<TCandidate, TSearchState, AlgorithmObservation<TCandidate, TSearchState>> target,
            out Duration duration, TimeProvider? timeProvider = null)
            where TSearchState : class, ISearchState
        {
            var created = new Duration(timeProvider);
            duration = created;
            return Substitution.Of(target.Algorithm, node => node.Timed(created));
        }
    }
}

using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Adds the time each call of its child algorithm spends advancing to its next state, including failures, to a duration. Uses the run's time provider.</summary>
/// <remarks>
/// The time between two states belongs to whoever consumes them, so pauses and the handling of a streamed state are not
/// measured. A terminator reading the same duration sees this time wherever the wrapper is placed.
/// </remarks>
public sealed record TimedAlgorithm<TCandidate, TSearchState> : WrappingAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public Duration Duration { get; init; }

    public static Parameter<TimedAlgorithm<TCandidate, TSearchState>, Duration> DurationParameter { get; } =
        new(nameof(Duration), static timed => timed.Duration, static (timed, value) => timed with { Duration = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [DurationParameter];

    public TimedAlgorithm(IAlgorithm<TCandidate, TSearchState> childAlgorithm, Duration duration)
        : base(childAlgorithm)
    {
        Duration = duration;
    }

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IAlgorithmExecutor<TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var childAlgorithm = scope.Resolve(ChildAlgorithm);
        var duration = scope.Resolve(Duration);
        return new Executor(childAlgorithm, duration, scope.Results.TimeProvider);
    }

    private sealed class Executor(IAlgorithmExecutor<TSearchState> childAlgorithm,
        DurationAccumulator duration, TimeProvider timeProvider)
        : WrappingAlgorithmExecutor<TSearchState>(childAlgorithm)
    {
        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await using var iterator = ChildAlgorithm.RunStreamingAsync(random, initialState, ct).GetAsyncEnumerator(ct);
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
        /// <summary>Adds the time this algorithm spends advancing to each state, including failures, to the duration. Uses the run's time provider.</summary>
        public TimedAlgorithm<TCandidate, TSearchState> Timed(Duration duration) => new(algorithm, duration);

        /// <summary>Measures an algorithm's advances wherever this configuration holds it, such as <c>ga.Timing(ga.Iterations, out var time)</c>. Uses the run's time provider.</summary>
        /// <param name="target">The iterations to measure. Their algorithm is matched by reference, and it is an error when this configuration does not hold it.</param>
        /// <param name="duration">Receives the new duration, which <c>run.Result(duration).CurrentDuration</c> reads and a terminator can check.</param>
        /// <returns>The interface, because the target may be this algorithm itself, which the wrapper then replaces.</returns>
        public IAlgorithm<TCandidate, TSearchState> Timing(AlgorithmSource<TCandidate, TSearchState, AlgorithmObservation<TCandidate, TSearchState>> target,
            out Duration duration)
        {
            var created = new Duration();
            duration = created;
            return algorithm.Substitute(target.Timing(created));
        }
    }

    extension<TCandidate, TSearchState>(AlgorithmSource<TCandidate, TSearchState, AlgorithmObservation<TCandidate, TSearchState>> iterations)
        where TSearchState : class, ISearchState
    {
        /// <summary>Places a wrapper adding time spent advancing to each state, including failures, to the duration. Uses the run's time provider.</summary>
        /// <remarks>
        /// In one substitution list, a later part sits closer to its target. List this placement after every analyzer that
        /// reads the duration so its reading includes the call; listed before them, it reads before the call.
        /// </remarks>
        public Substitution Timing(Duration duration) => Substitution.Of(iterations.Algorithm, node => node.Timed(duration));
    }
}

using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Measures iteration advances, including failures, while excluding consumer pauses.</summary>
public sealed class DurationMeasuringAlgorithmAdvice<TCandidate, TSearchState> : AlgorithmAdvice<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public DurationAccumulator Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringAlgorithmAdvice(DurationAccumulator duration)
        : this(duration, TimeProvider.System)
    {
    }

    public DurationMeasuringAlgorithmAdvice(DurationAccumulator duration, TimeProvider timeProvider)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    public override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Wrap<TSearchSpace, TProblem>(
        IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, IAlgorithm<TCandidate, TSearchState> source) =>
        new Executor<TSearchSpace, TProblem>(executor, Duration, TimeProvider);

    private sealed class Executor<TSearchSpace, TProblem>(IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm, DurationAccumulator duration, TimeProvider timeProvider)
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

public static class AlgorithmDurationExtensions
{
    extension(Aspect)
    {
        public static Aspect MeasureDuration<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm, DurationAccumulator duration)
            where TSearchState : class, ISearchState =>
            Aspect.Create(Pointcut.Reference(algorithm), new DurationMeasuringAlgorithmAdvice<TCandidate, TSearchState>(duration));

        public static Aspect MeasureDuration<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm, DurationAccumulator duration, TimeProvider timeProvider)
            where TSearchState : class, ISearchState =>
            Aspect.Create(Pointcut.Reference(algorithm), new DurationMeasuringAlgorithmAdvice<TCandidate, TSearchState>(duration, timeProvider));
    }
}

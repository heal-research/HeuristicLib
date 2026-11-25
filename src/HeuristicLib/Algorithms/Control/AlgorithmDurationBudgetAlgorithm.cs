using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record AlgorithmDurationBudgetAlgorithm<TCandidate, TSearchState>
    : Algorithm<AlgorithmDurationBudgetAlgorithm<TCandidate, TSearchState>, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public required IAlgorithm<TCandidate, TSearchState> Algorithm { get; init; }

    /// <summary>
    /// Gets the duration budget measured with the run's time provider. The expected value is positive.
    /// </summary>
    /// <remarks>The budget is checked after each produced state, so a nonpositive budget stops after the first state.</remarks>
    public TimeSpan MaximumDuration { get; init; }

    public static Parameter<AlgorithmDurationBudgetAlgorithm<TCandidate, TSearchState>, IAlgorithm<TCandidate, TSearchState>> AlgorithmParameter { get; } =
        new(nameof(Algorithm), static algorithm => algorithm.Algorithm, static (algorithm, value) => algorithm with { Algorithm = value });

    public static Parameter<AlgorithmDurationBudgetAlgorithm<TCandidate, TSearchState>, TimeSpan> MaximumDurationParameter { get; } =
        new(nameof(MaximumDuration), static algorithm => algorithm.MaximumDuration, static (algorithm, value) => algorithm with { MaximumDuration = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [AlgorithmParameter, MaximumDurationParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IAlgorithmExecutor<TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var algorithm = scope.Resolve(Algorithm);
        return new Executor(algorithm, MaximumDuration, scope.Results.TimeProvider);
    }

    private sealed class Executor
        : AlgorithmExecutor<TSearchState>
    {
        private readonly IAlgorithmExecutor<TSearchState> algorithm;
        private readonly TimeSpan maximumDuration;
        private readonly TimeProvider timeProvider;

        public Executor(IAlgorithmExecutor<TSearchState> algorithm, TimeSpan maximumDuration, TimeProvider timeProvider)
        {
            this.algorithm = algorithm;
            this.maximumDuration = maximumDuration;
            this.timeProvider = timeProvider;
        }

        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var duration = TimeSpan.Zero;
            await using var enumerator = algorithm
                .RunStreamingAsync(random, initialState, ct)
                .GetAsyncEnumerator(ct);

            while (true)
            {
                var startTimestamp = timeProvider.GetTimestamp();
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                finally
                {
                    duration += timeProvider.GetElapsedTime(startTimestamp);
                }

                if (!hasNext)
                {
                    yield break;
                }

                yield return enumerator.Current;

                if (duration >= maximumDuration)
                {
                    yield break;
                }
            }
        }
    }
}

public static class AlgorithmDurationBudgetExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>Stops after the produced state that reaches the duration budget, measured with the run's time provider.</summary>
        public AlgorithmDurationBudgetAlgorithm<TCandidate, TSearchState> LimitedToDuration(TimeSpan maximumDuration) =>
            new()
            {
                Algorithm = algorithm,
                MaximumDuration = maximumDuration
            };
    }
}

using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public record AlgorithmDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Algorithm<AlgorithmDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>, TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public required IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm { get; init; }
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>
    /// Gets the duration budget. The expected value is positive.
    /// </summary>
    /// <remarks>The budget is checked after each produced state, so a nonpositive budget stops after the first state.</remarks>
    public TimeSpan MaximumDuration { get; init; }

    public override AlgorithmDurationBudgetAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new(instanceRegistry.Resolve(Algorithm), MaximumDuration, TimeProvider);
}

public sealed class AlgorithmDurationBudgetAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm;
    private readonly TimeSpan maximumDuration;
    private readonly TimeProvider timeProvider;

    public AlgorithmDurationBudgetAlgorithmInstance(IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, TimeSpan maximumDuration, TimeProvider timeProvider)
    {
        this.algorithm = algorithm;
        this.maximumDuration = maximumDuration;
        this.timeProvider = timeProvider;
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var duration = TimeSpan.Zero;
        await using var enumerator = algorithm
            .RunStreamingAsync(problem, random, initialState, ct)
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

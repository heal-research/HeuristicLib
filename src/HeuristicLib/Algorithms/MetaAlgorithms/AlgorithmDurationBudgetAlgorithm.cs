using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public record AlgorithmDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public required IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm { get; init; }
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    public TimeSpan MaximumDuration
    {
        get;
        init => field = value > TimeSpan.Zero
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MaximumDuration), "MaximumDuration must be positive.");
    }

    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator => Algorithm.Evaluator;

    public IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    {
        return new AlgorithmDurationBudgetAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(
            instanceRegistry.Resolve(Algorithm),
            MaximumDuration,
            TimeProvider);
    }
}

public sealed class AlgorithmDurationBudgetAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm;
    private readonly TimeSpan maximumDuration;
    private readonly TimeProvider timeProvider;

    public AlgorithmDurationBudgetAlgorithmInstance(
        IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm,
        TimeSpan maximumDuration,
        TimeProvider timeProvider)
    {
        this.algorithm = algorithm;
        this.maximumDuration = maximumDuration;
        this.timeProvider = timeProvider;
    }

    public async IAsyncEnumerable<TSearchState> RunStreamingAsync(
        TProblem problem,
        IRandomNumberGenerator random,
        TSearchState? initialState = null,
        [EnumeratorCancellation] CancellationToken ct = default)
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

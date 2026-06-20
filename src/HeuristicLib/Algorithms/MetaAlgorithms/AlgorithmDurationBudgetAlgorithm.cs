using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public record AlgorithmDurationBudgetAlgorithm<TG, TS, TP, TSearchState>
    : IAlgorithm<TG, TS, TP, TSearchState>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TSearchState : class, ISearchState
{
    public required IAlgorithm<TG, TS, TP, TSearchState> Algorithm { get; init; }
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    public TimeSpan MaximumDuration
    {
        get;
        init => field = value > TimeSpan.Zero
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MaximumDuration), "MaximumDuration must be positive.");
    }

    public IEvaluator<TG, TS, TP> Evaluator => Algorithm.Evaluator;

    public IAlgorithmInstance<TG, TS, TP, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    {
        return new AlgorithmDurationBudgetAlgorithmInstance<TG, TS, TP, TSearchState>(
            instanceRegistry.Resolve(Algorithm),
            MaximumDuration,
            TimeProvider);
    }
}

public sealed class AlgorithmDurationBudgetAlgorithmInstance<TG, TS, TP, TSearchState>
    : IAlgorithmInstance<TG, TS, TP, TSearchState>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithmInstance<TG, TS, TP, TSearchState> algorithm;
    private readonly TimeSpan maximumDuration;
    private readonly TimeProvider timeProvider;

    public AlgorithmDurationBudgetAlgorithmInstance(
        IAlgorithmInstance<TG, TS, TP, TSearchState> algorithm,
        TimeSpan maximumDuration,
        TimeProvider timeProvider)
    {
        this.algorithm = algorithm;
        this.maximumDuration = maximumDuration;
        this.timeProvider = timeProvider;
    }

    public async IAsyncEnumerable<TSearchState> RunStreamingAsync(
        TP problem,
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

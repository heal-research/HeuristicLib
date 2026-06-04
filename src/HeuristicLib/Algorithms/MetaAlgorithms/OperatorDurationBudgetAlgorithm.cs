using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public record OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, TOperator, TObservedInstance>
    : IAlgorithm<TG, TS, TP, TSearchState>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TSearchState : class, ISearchState
    where TOperator : IOperator<TObservedInstance>
    where TObservedInstance : class, IOperatorInstance
{
    public required IAlgorithm<TG, TS, TP, TSearchState> Algorithm { get; init; }
    public required TOperator ObservedOperator { get; init; }
    public required Func<TOperator, ObservationDuration, TimeProvider, IOperator<TObservedInstance>> MeasuredOperatorFactory { get; init; }
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
        var duration = new ObservationDuration();
        var measuredOperator = MeasuredOperatorFactory(ObservedOperator, duration, TimeProvider);
        var childRegistry = instanceRegistry.CreateChildRegistry();
        childRegistry.PreRegister(ObservedOperator, measuredOperator);

        return new OperatorDurationBudgetAlgorithmInstance<TG, TS, TP, TSearchState>(
            childRegistry.Resolve(Algorithm),
            duration,
            MaximumDuration);
    }
}

public sealed class OperatorDurationBudgetAlgorithmInstance<TG, TS, TP, TSearchState>
    : IAlgorithmInstance<TG, TS, TP, TSearchState>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithmInstance<TG, TS, TP, TSearchState> algorithm;
    private readonly ObservationDuration duration;
    private readonly TimeSpan maximumDuration;

    public OperatorDurationBudgetAlgorithmInstance(
        IAlgorithmInstance<TG, TS, TP, TSearchState> algorithm,
        ObservationDuration duration,
        TimeSpan maximumDuration)
    {
        this.algorithm = algorithm;
        this.duration = duration;
        this.maximumDuration = maximumDuration;
    }

    public async IAsyncEnumerable<TSearchState> RunStreamingAsync(
        TP problem,
        IRandomNumberGenerator random,
        TSearchState? initialState = null,
        [EnumeratorCancellation] CancellationToken ct = default)
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

public static class OperatorDurationBudgetAlgorithmExtensions
{
    extension<TG, TS, TP, TSearchState>(IAlgorithm<TG, TS, TP, TSearchState> algorithm)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
        where TSearchState : class, ISearchState
    {
        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, IEvaluator<TG, TS, TP>, IEvaluatorInstance<TG, TS, TP>> WithMaxEvaluatorDuration(
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxEvaluatorDuration(maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, IEvaluator<TG, TS, TP>, IEvaluatorInstance<TG, TS, TP>> WithMaxEvaluatorDuration(
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return new OperatorDurationBudgetAlgorithm<
                TG,
                TS,
                TP,
                TSearchState,
                IEvaluator<TG, TS, TP>,
                IEvaluatorInstance<TG, TS, TP>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Evaluator,
                MaximumDuration = maximumDuration,
                TimeProvider = timeProvider,
                MeasuredOperatorFactory = static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureEvaluatorDuration(duration, timeProvider)
            };
        }
    }
}

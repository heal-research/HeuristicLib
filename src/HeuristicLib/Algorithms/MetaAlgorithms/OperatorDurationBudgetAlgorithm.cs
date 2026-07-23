using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public record OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TOperator, TObservedInstance>
    : Algorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TOperator : IOperator<TObservedInstance>
    where TObservedInstance : class, IOperatorInstance
{
    public required IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm { get; init; }
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

    protected override OperatorDurationBudgetAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateAlgorithmInstance(ExecutionInstanceRegistry registry)
    {
        var duration = new ObservationDuration();
        var measuredOperator = MeasuredOperatorFactory(ObservedOperator, duration, TimeProvider);
        var childRegistry = registry.CreateChildRegistry();
        childRegistry.RegisterReplacement(ObservedOperator, measuredOperator);

        return new OperatorDurationBudgetAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childRegistry.Resolve(Algorithm), duration, MaximumDuration);
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

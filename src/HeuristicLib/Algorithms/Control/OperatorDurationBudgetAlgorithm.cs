using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <remarks>
/// Sums invocation durations for every operation of the selected configuration references in the resolution scope the
/// budget is built in and in its child scopes, including calls from outside the child algorithm. Repeated references
/// count once. Nested and concurrent calls contribute their full durations, including failed calls.
/// Each executor owns one combined accumulator, checked after each produced state.
/// </remarks>
public record OperatorDurationBudgetAlgorithm<TCandidate, TSearchState>
    : Algorithm<OperatorDurationBudgetAlgorithm<TCandidate, TSearchState>, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public IAlgorithm<TCandidate, TSearchState> Algorithm { get; init; }
    public ValueArray<IOperator> ObservedOperators { get; init; }
    public TimeSpan MaximumDuration { get; init; }
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    public OperatorDurationBudgetAlgorithm(IAlgorithm<TCandidate, TSearchState> algorithm, IReadOnlyList<IOperator> observedOperators, TimeSpan maximumDuration)
    {
        Algorithm = algorithm;
        ObservedOperators = observedOperators.ToValueArray();
        MaximumDuration = maximumDuration;
    }

    public override bool Fits(ExecutionSignature execution) =>
        base.Fits(execution) && execution.Fits(Algorithm) && ObservedOperators.All(source => source.Fits(execution));

    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var accumulator = new DurationAccumulator();
        var selection = ObservedOperators.Aggregate(Pointcut.OfType<IOperator>().Not(),
            (pointcut, source) => pointcut | Pointcut.Reference(source));
        var aspect = Aspect.Create(selection, new DurationMeasuringOperatorAdvice(accumulator, TimeProvider));
        scope.AddAspect(aspect);
        var algorithm = scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(Algorithm);
        return new OperatorDurationBudgetAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, accumulator, MaximumDuration);
    }
}

public record OperatorDurationBudgetAlgorithm<TOperation, TCandidate, TSearchState>
    : Algorithm<OperatorDurationBudgetAlgorithm<TOperation, TCandidate, TSearchState>, TCandidate, TSearchState>
    where TOperation : IOperation<TOperation>
    where TSearchState : class, ISearchState
{
    public IAlgorithm<TCandidate, TSearchState> Algorithm { get; init; }

    /// <summary>
    /// Gets the operator whose call duration, including failed calls, is measured against the budget.
    /// </summary>
    /// <remarks>
    /// Measurement applies to every call of the operator in the budget's resolution scope and its child scopes, so
    /// enclosing budgets and observations compose. If nothing in that scope calls the observed operator, nothing is
    /// measured and the budget never stops.
    /// </remarks>
    public IOperator<TOperation, TCandidate> ObservedOperator { get; init; }

    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>
    /// Gets the measured-operator duration budget. The expected value is positive.
    /// </summary>
    /// <remarks>The budget is checked after each produced state, so a nonpositive budget stops after the first state.</remarks>
    public TimeSpan MaximumDuration { get; init; }

    public OperatorDurationBudgetAlgorithm(IAlgorithm<TCandidate, TSearchState> algorithm, IOperator<TOperation, TCandidate> observedOperator, TimeSpan maximumDuration)
    {
        Algorithm = algorithm;
        ObservedOperator = observedOperator;
        MaximumDuration = maximumDuration;
    }

    public override bool Fits(ExecutionSignature execution) => base.Fits(execution) && execution.Fits(Algorithm, ObservedOperator);

    /// <remarks>Each executor has its own accumulator and contextual timing advice.</remarks>
    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var accumulator = new DurationAccumulator();
        var aspect = Aspect.Create(Pointcut.Reference(ObservedOperator), new DurationMeasuringOperatorAdvice<TOperation, TCandidate>(accumulator, TimeProvider));
        scope.AddAspect(aspect);
        var algorithm = scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(Algorithm);
        return new OperatorDurationBudgetAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, accumulator, MaximumDuration);
    }
}

public sealed class OperatorDurationBudgetAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm;
    private readonly DurationAccumulator duration;
    private readonly TimeSpan maximumDuration;

    public OperatorDurationBudgetAlgorithmExecutor(IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, DurationAccumulator duration, TimeSpan maximumDuration)
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

public static class OperatorDurationBudgetExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <remarks>Measures all operations of each selected configuration. Repeated references count once.</remarks>
        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState> LimitedToDuration(
            IReadOnlyList<IOperator> observedOperators, TimeSpan maximumDuration) =>
            new(algorithm, observedOperators, maximumDuration) { TimeProvider = TimeProvider.System };

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState> LimitedToDuration(
            IReadOnlyList<IOperator> observedOperators, TimeSpan maximumDuration, TimeProvider timeProvider) =>
            new(algorithm, observedOperators, maximumDuration) { TimeProvider = timeProvider };

        /// <summary>Stops the algorithm once the operator's calls, including failed ones, have taken the given time.</summary>
        /// <remarks>
        /// The operation is inferred from the operator; for an operator implementing several operations, pass it through a
        /// variable of the operation's operator type.
        /// </remarks>
        public OperatorDurationBudgetAlgorithm<TOperation, TCandidate, TSearchState> LimitedToDuration<TOperation>(
            IOperator<TOperation, TCandidate> observedOperator, TimeSpan maximumDuration)
            where TOperation : IOperation<TOperation>
        {
            return new(algorithm, observedOperator, maximumDuration) { TimeProvider = TimeProvider.System };
        }

        /// <summary>Stops the algorithm once the operator's calls, including failed ones, have taken the given time.</summary>
        /// <remarks>
        /// The operation is inferred from the operator; for an operator implementing several operations, pass it through a
        /// variable of the operation's operator type.
        /// </remarks>
        public OperatorDurationBudgetAlgorithm<TOperation, TCandidate, TSearchState> LimitedToDuration<TOperation>(
            IOperator<TOperation, TCandidate> observedOperator, TimeSpan maximumDuration, TimeProvider timeProvider)
            where TOperation : IOperation<TOperation>
        {
            return new(algorithm, observedOperator, maximumDuration) { TimeProvider = timeProvider };
        }
    }
}

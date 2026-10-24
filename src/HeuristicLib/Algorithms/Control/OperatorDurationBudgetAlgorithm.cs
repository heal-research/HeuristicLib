using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record OperatorDurationBudgetAlgorithm<TOperation, TCandidate, TSearchState>
    : Algorithm<OperatorDurationBudgetAlgorithm<TOperation, TCandidate, TSearchState>, TCandidate, TSearchState>
    where TOperation : IOperation<TOperation>
    where TSearchState : class, ISearchState
{
    public required IAlgorithm<TCandidate, TSearchState> Algorithm { get; init; }

    /// <summary>
    /// Gets the operator whose call duration, including failed calls, is measured against the budget.
    /// </summary>
    /// <remarks>
    /// Measurement applies only inside this budget's algorithm, so enclosing budgets and observations compose. If the
    /// algorithm does not reference the observed operator, nothing is measured and the budget never stops.
    /// </remarks>
    public required IOperator<TOperation, TCandidate> ObservedOperator { get; init; }

    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>
    /// Gets the measured-operator duration budget. The expected value is positive.
    /// </summary>
    /// <remarks>The budget is checked after each produced state, so a nonpositive budget stops after the first state.</remarks>
    public TimeSpan MaximumDuration { get; init; }

    public override bool Fits(ExecutionSignature execution) => base.Fits(execution) && execution.Fits(Algorithm, ObservedOperator);

    public override ExecutorFactory<IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
    {
        var duration = new DurationAccumulator();
        var measuring = Aspect.Create(Pointcut.Reference(ObservedOperator), new DurationMeasuringOperatorAdvice<TOperation, TCandidate>(duration, TimeProvider));
        return scope =>
        {
            var childScope = scope.With(measuring);
            return new OperatorDurationBudgetAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(childScope.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(Algorithm), duration, MaximumDuration);
        };
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
        /// <summary>Stops the algorithm once the operator's calls, including failed ones, have taken the given time.</summary>
        /// <remarks>
        /// The operation is inferred from the operator; for an operator implementing several operations, pass it through a
        /// variable of the operation's operator type.
        /// </remarks>
        public OperatorDurationBudgetAlgorithm<TOperation, TCandidate, TSearchState> LimitedToDuration<TOperation>(
            IOperator<TOperation, TCandidate> observedOperator, TimeSpan maximumDuration)
            where TOperation : IOperation<TOperation>
        {
            return algorithm.LimitedToDuration(observedOperator, maximumDuration, TimeProvider.System);
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
            return new()
            {
                Algorithm = algorithm,
                ObservedOperator = observedOperator,
                MaximumDuration = maximumDuration,
                TimeProvider = timeProvider
            };
        }
    }
}

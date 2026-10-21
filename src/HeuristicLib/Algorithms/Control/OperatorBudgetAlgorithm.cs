using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record OperatorBudgetAlgorithm<TOperation, TCandidate, TSearchState>
    : Algorithm<OperatorBudgetAlgorithm<TOperation, TCandidate, TSearchState>, TCandidate, TSearchState>
    where TOperation : IOperationDescriptor<TOperation>
    where TSearchState : class, ISearchState
{
    public required IAlgorithm<TCandidate, TSearchState> Algorithm { get; init; }

    /// <summary>
    /// Gets the operator whose work is counted against the budget.
    /// </summary>
    /// <remarks>
    /// Counting applies only inside this budget's algorithm, so enclosing budgets and observations compose. If the
    /// algorithm does not reference the observed operator, nothing is counted and the budget never stops.
    /// </remarks>
    public required IOperator<TOperation, TCandidate> ObservedOperator { get; init; }

    /// <summary>
    /// Gets whether the budget counts the observed operator's successful calls or its candidate work.
    /// </summary>
    /// <remarks>Counting candidates fails when the run is built if the operation reports no candidate work.</remarks>
    public OperatorCountMetric Metric { get; init; } = OperatorCountMetric.Calls;

    /// <summary>
    /// Gets the counted-operator budget. The expected value is positive.
    /// </summary>
    /// <remarks>The budget is checked after each produced state, so a nonpositive budget stops after the first state.</remarks>
    public int MaximumCount { get; init; }

    public override bool Fits(ExecutionSignature execution) => base.Fits(execution) && execution.Fits(Algorithm, ObservedOperator);

    public override ExecutorFactory<IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
    {
        var counter = new CountAccumulator();
        var counting = Aspect.Create(Pointcut.Reference(ObservedOperator), new CountingOperatorAdvice<TOperation, TCandidate>(counter, Metric));
        return scope =>
        {
            var childScope = scope.With(counting);
            return new OperatorBudgetAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(childScope.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(Algorithm), counter, MaximumCount);
        };
    }
}

public sealed class OperatorBudgetAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm;
    private readonly CountAccumulator counter;
    private readonly int maximumCount;

    public OperatorBudgetAlgorithmExecutor(IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, CountAccumulator counter, int maximumCount)
    {
        this.algorithm = algorithm;
        this.counter = counter;
        this.maximumCount = maximumCount;
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var state in algorithm.RunStreamingAsync(problem, random, initialState, ct))
        {
            yield return state;

            if (counter.CurrentCount >= maximumCount)
            {
                yield break;
            }
        }
    }
}

public static class OperatorCountBudgetExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>Stops the algorithm once the operator has completed the given number of calls.</summary>
        /// <remarks>
        /// The operation is inferred from the operator; for an operator implementing several operations, pass it through a
        /// variable of the operation's operator type.
        /// </remarks>
        public OperatorBudgetAlgorithm<TOperation, TCandidate, TSearchState> LimitedToCalls<TOperation>(
            IOperator<TOperation, TCandidate> observedOperator, int maximumCalls)
            where TOperation : IOperationDescriptor<TOperation>
        {
            return new()
            {
                Algorithm = algorithm,
                ObservedOperator = observedOperator,
                Metric = OperatorCountMetric.Calls,
                MaximumCount = maximumCalls
            };
        }

        /// <summary>Stops the algorithm once the operator's successful calls have processed the given number of candidates.</summary>
        /// <remarks>
        /// The operation is inferred from the operator; for an operator implementing several operations, pass it through a
        /// variable of the operation's operator type. An operation without candidate work, such as termination, fails when
        /// the run is built.
        /// </remarks>
        public OperatorBudgetAlgorithm<TOperation, TCandidate, TSearchState> LimitedToCandidates<TOperation>(
            IOperator<TOperation, TCandidate> observedOperator, int maximumCandidates)
            where TOperation : IOperationDescriptor<TOperation>
        {
            return new()
            {
                Algorithm = algorithm,
                ObservedOperator = observedOperator,
                Metric = OperatorCountMetric.Candidates,
                MaximumCount = maximumCandidates
            };
        }
    }
}

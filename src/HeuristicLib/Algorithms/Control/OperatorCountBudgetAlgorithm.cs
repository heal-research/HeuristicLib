using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <remarks>
/// Counts every operation of the selected configuration references in the resolution scope the budget is built in and
/// in its child scopes, including calls from outside the child algorithm, such as a terminator's. Repeated references
/// count once. Each executor owns one combined accumulator, checked after each produced state.
/// </remarks>
public record OperatorCountBudgetAlgorithm<TCandidate, TSearchState>
    : Algorithm<OperatorCountBudgetAlgorithm<TCandidate, TSearchState>, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public IAlgorithm<TCandidate, TSearchState> Algorithm { get; init; }
    public ValueArray<IOperator> ObservedOperators { get; init; }
    public int MaximumCount { get; init; }

    /// <remarks>Candidate counting rejects a selected operation without candidate work when it is resolved.</remarks>
    public OperatorCountMetric Metric { get; init; } = OperatorCountMetric.Invocations;

    public OperatorCountBudgetAlgorithm(IAlgorithm<TCandidate, TSearchState> algorithm, IReadOnlyList<IOperator> observedOperators, int maximumCount)
    {
        Algorithm = algorithm;
        ObservedOperators = observedOperators.ToValueArray();
        MaximumCount = maximumCount;
    }

    public override bool Fits(ExecutionSignature execution) =>
        base.Fits(execution) && execution.Fits(Algorithm) && ObservedOperators.All(source => source.Fits(execution));

    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var accumulator = new CountAccumulator();
        var selection = ObservedOperators.Aggregate(Pointcut.OfType<IOperator>().Not(),
            (pointcut, source) => pointcut | Pointcut.Reference(source));
        var aspect = Aspect.Create(selection, new CountingOperatorAdvice(accumulator, Metric));
        scope.AddAspect(aspect);
        var algorithm = scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(Algorithm);
        return new OperatorCountBudgetAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, accumulator, MaximumCount);
    }
}

public record OperatorCountBudgetAlgorithm<TOperation, TCandidate, TSearchState>
    : Algorithm<OperatorCountBudgetAlgorithm<TOperation, TCandidate, TSearchState>, TCandidate, TSearchState>
    where TOperation : IOperation<TOperation>
    where TSearchState : class, ISearchState
{
    public IAlgorithm<TCandidate, TSearchState> Algorithm { get; init; }

    /// <summary>
    /// Gets the operator whose work is counted against the budget.
    /// </summary>
    /// <remarks>
    /// Counting applies to every call of the operator in the budget's resolution scope and its child scopes, so
    /// enclosing budgets and observations compose. If nothing in that scope calls the observed operator, nothing is
    /// counted and the budget never stops.
    /// </remarks>
    public IOperator<TOperation, TCandidate> ObservedOperator { get; init; }

    /// <summary>
    /// Gets whether the budget counts the observed operator's successful calls or its candidate work.
    /// </summary>
    /// <remarks>Counting candidates fails when the run is built if the operation reports no candidate work.</remarks>
    public OperatorCountMetric Metric { get; init; } = OperatorCountMetric.Invocations;

    /// <summary>
    /// Gets the counted-operator budget. The expected value is positive.
    /// </summary>
    /// <remarks>The budget is checked after each produced state, so a nonpositive budget stops after the first state.</remarks>
    public int MaximumCount { get; init; }

    public OperatorCountBudgetAlgorithm(IAlgorithm<TCandidate, TSearchState> algorithm, IOperator<TOperation, TCandidate> observedOperator, int maximumCount)
    {
        Algorithm = algorithm;
        ObservedOperator = observedOperator;
        MaximumCount = maximumCount;
    }

    public override bool Fits(ExecutionSignature execution) => base.Fits(execution) && execution.Fits(Algorithm, ObservedOperator);

    /// <remarks>Each executor has its own accumulator and contextual counting advice.</remarks>
    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var accumulator = new CountAccumulator();
        var aspect = Aspect.Create(Pointcut.Reference(ObservedOperator), new CountingOperatorAdvice<TOperation, TCandidate>(accumulator, Metric));
        scope.AddAspect(aspect);
        var algorithm = scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(Algorithm);
        return new OperatorCountBudgetAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, accumulator, MaximumCount);
    }
}

public sealed class OperatorCountBudgetAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm;
    private readonly CountAccumulator counter;
    private readonly int maximumCount;

    public OperatorCountBudgetAlgorithmExecutor(IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, CountAccumulator counter, int maximumCount)
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
        /// <remarks>Counts all operations of each selected configuration. Repeated references count once.</remarks>
        public OperatorCountBudgetAlgorithm<TCandidate, TSearchState> LimitedToInvocations(
            IReadOnlyList<IOperator> observedOperators, int maximumInvocations) =>
            new(algorithm, observedOperators, maximumInvocations) { Metric = OperatorCountMetric.Invocations };

        /// <remarks>Counts all operations of each selected configuration. Each selected operation must report candidate work.</remarks>
        public OperatorCountBudgetAlgorithm<TCandidate, TSearchState> LimitedToCandidates(
            IReadOnlyList<IOperator> observedOperators, int maximumCandidates) =>
            new(algorithm, observedOperators, maximumCandidates) { Metric = OperatorCountMetric.Candidates };

        /// <summary>Stops the algorithm once the operator has completed the given number of calls.</summary>
        /// <remarks>
        /// The operation is inferred from the operator; for an operator implementing several operations, pass it through a
        /// variable of the operation's operator type.
        /// </remarks>
        public OperatorCountBudgetAlgorithm<TOperation, TCandidate, TSearchState> LimitedToInvocations<TOperation>(
            IOperator<TOperation, TCandidate> observedOperator, int maximumInvocations)
            where TOperation : IOperation<TOperation>
        {
            return new(algorithm, observedOperator, maximumInvocations) { Metric = OperatorCountMetric.Invocations };
        }

        /// <summary>Stops the algorithm once the operator's successful calls have processed the given number of candidates.</summary>
        /// <remarks>
        /// The operation is inferred from the operator; for an operator implementing several operations, pass it through a
        /// variable of the operation's operator type. An operation without candidate work, such as termination, fails when
        /// the run is built.
        /// </remarks>
        public OperatorCountBudgetAlgorithm<TOperation, TCandidate, TSearchState> LimitedToCandidates<TOperation>(
            IOperator<TOperation, TCandidate> observedOperator, int maximumCandidates)
            where TOperation : IOperation<TOperation>
        {
            return new(algorithm, observedOperator, maximumCandidates) { Metric = OperatorCountMetric.Candidates };
        }
    }
}

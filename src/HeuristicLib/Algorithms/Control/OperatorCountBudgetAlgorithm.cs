using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>Stops its algorithm once a counter reaches a maximum count, checked after each produced state.</summary>
/// <remarks>
/// The counter restarts each time this budget is built, such as in each Cycle turn, because the budget shares it in its own
/// scope. The wrappers counting into it live in <see cref="Algorithm"/>, where <c>LimitedToInvocations</c>,
/// <c>LimitedToCandidates</c> and <c>LimitedToCount</c> place them. A copy that replaces <see cref="Algorithm"/> loses those
/// wrappers, and the budget then never stops.
/// </remarks>
public sealed record OperatorCountBudgetAlgorithm<TCandidate, TSearchState>
    : Algorithm<OperatorCountBudgetAlgorithm<TCandidate, TSearchState>, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public OperatorCountBudgetAlgorithm(IAlgorithm<TCandidate, TSearchState> algorithm, Counter counter, int maximumCount)
    {
        Algorithm = algorithm;
        Counter = counter;
        MaximumCount = maximumCount;
    }

    public IAlgorithm<TCandidate, TSearchState> Algorithm { get; init; }

    public Counter Counter { get; init; }

    /// <summary>
    /// Gets the counted-operator budget. The expected value is positive.
    /// </summary>
    /// <remarks>The budget is checked after each produced state, so a nonpositive budget stops after the first state.</remarks>
    public int MaximumCount { get; init; }

    public static Parameter<OperatorCountBudgetAlgorithm<TCandidate, TSearchState>, IAlgorithm<TCandidate, TSearchState>> AlgorithmParameter { get; } =
        new(nameof(Algorithm), static budget => budget.Algorithm, static (budget, value) => budget with { Algorithm = value });

    public static Parameter<OperatorCountBudgetAlgorithm<TCandidate, TSearchState>, Counter> CounterParameter { get; } =
        new(nameof(Counter), static budget => budget.Counter, static (budget, value) => budget with { Counter = value });

    public static Parameter<OperatorCountBudgetAlgorithm<TCandidate, TSearchState>, int> MaximumCountParameter { get; } =
        new(nameof(MaximumCount), static budget => budget.MaximumCount, static (budget, value) => budget with { MaximumCount = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [AlgorithmParameter, CounterParameter, MaximumCountParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IAlgorithmExecutor<TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        scope.Share(Counter);
        var algorithm = scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(Algorithm, scope.Problem);
        return new OperatorCountBudgetAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, scope.Resolve(Counter), MaximumCount, scope.Problem);
    }
}

public sealed class OperatorCountBudgetAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithmExecutor<TSearchState> algorithm;
    private readonly CountAccumulator counter;
    private readonly int maximumCount;

    public OperatorCountBudgetAlgorithmExecutor(IAlgorithmExecutor<TSearchState> algorithm, CountAccumulator counter, int maximumCount, TProblem problem)
        : base(problem)
    {
        this.algorithm = algorithm;
        this.counter = counter;
        this.maximumCount = maximumCount;
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var state in algorithm.RunStreamingAsync(random, initialState, ct))
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
        /// The operator is matched by reference, and it is an error when the algorithm does not hold it. The operation is
        /// inferred from the operator; for an operator implementing several operations, pass it through a variable of the
        /// operation's operator type.
        /// </remarks>
        public OperatorCountBudgetAlgorithm<TCandidate, TSearchState> LimitedToInvocations<TOperation>(
            IOperator<TOperation, TCandidate> observedOperator, int maximumInvocations)
            where TOperation : IOperation<TOperation> =>
            algorithm.LimitedTo(observedOperator, OperatorCountMetric.Invocations, maximumInvocations);

        /// <summary>Stops the algorithm once the operator's successful calls have processed the given number of candidates.</summary>
        /// <remarks>
        /// The operator is matched by reference, and it is an error when the algorithm does not hold it. The operation is
        /// inferred from the operator; for an operator implementing several operations, pass it through a variable of the
        /// operation's operator type. An operation without candidate work, such as termination, fails when the run is built.
        /// </remarks>
        public OperatorCountBudgetAlgorithm<TCandidate, TSearchState> LimitedToCandidates<TOperation>(
            IOperator<TOperation, TCandidate> observedOperator, int maximumCandidates)
            where TOperation : IOperation<TOperation> =>
            algorithm.LimitedTo(observedOperator, OperatorCountMetric.Candidates, maximumCandidates);

        private OperatorCountBudgetAlgorithm<TCandidate, TSearchState> LimitedTo<TOperation>(
            IOperator<TOperation, TCandidate> observedOperator, OperatorCountMetric metric, int maximumCount)
            where TOperation : IOperation<TOperation> =>
            new(algorithm.Counting(observedOperator, out var counter, metric), counter, maximumCount);
    }

    extension<TAlgorithm>(TAlgorithm algorithm) where TAlgorithm : class, IConfigurationNode
    {
        /// <summary>
        /// Stops the algorithm once the counts that <paramref name="placement"/> places reach the given number, such as
        /// <c>ga.LimitedToCount(1_000, (g, calls) => g.Counting(g.Mutator, calls).Counting(g.Crossover, calls))</c>.
        /// </summary>
        /// <remarks>
        /// The placement runs during this call and is not stored. It states what each wrapper counts, so one budget can add
        /// calls of one operator to candidates of another. Counting a target twice counts every call twice. The counter
        /// restarts with the budget and stays inside it; a placement that ignores it leaves a budget that never stops.
        /// </remarks>
        /// <param name="maximumCount">The count after which the budget stops, checked after each produced state.</param>
        /// <param name="placement">Places the counting wrappers on the algorithm it receives, which is this one, and returns the algorithm to limit.</param>
        public OperatorCountBudgetAlgorithm<TCandidate, TSearchState> LimitedToCount<TCandidate, TSearchState>(int maximumCount,
            Func<TAlgorithm, Counter, IAlgorithm<TCandidate, TSearchState>> placement)
            where TSearchState : class, ISearchState
        {
            var counter = new Counter();
            return new(placement(algorithm, counter), counter, maximumCount);
        }
    }
}

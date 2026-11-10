using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Counts the successful calls of its child, or their candidate work, into a counter.</summary>
/// <remarks>
/// A terminator reading the same counter sees these calls wherever the wrapper is placed. A candidate counter on an
/// operation without candidate work fails when this configuration is built.
/// </remarks>
public sealed record Counted<TOperation, TCandidate> : WrappingOperator<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
{
    public Counted(IOperator<TOperation, TCandidate> childOperator, IConfigurationNode<CountAccumulator> counter, OperatorCountMetric metric = OperatorCountMetric.Invocations)
        : base(childOperator)
    {
        Counter = counter;
        Metric = metric;
    }

    public IConfigurationNode<CountAccumulator> Counter { get; init; }

    /// <summary>Whether the wrapper counts successful calls or their candidate work.</summary>
    public OperatorCountMetric Metric { get; init; }

    public static Parameter<Counted<TOperation, TCandidate>, IConfigurationNode<CountAccumulator>> CounterParameter { get; } =
        new(nameof(Counter), static counted => counted.Counter, static (counted, value) => counted with { Counter = value });

    public static Parameter<Counted<TOperation, TCandidate>, OperatorCountMetric> MetricParameter { get; } =
        new(nameof(Metric), static counted => counted.Metric, static (counted, value) => counted with { Metric = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [CounterParameter, MetricParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var operation = OperationInfo.For<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>();
        if (Metric == OperatorCountMetric.Candidates)
            operation.RequireCandidateWork(ChildOperator, "Counting candidates");
        var counter = scope.Resolve(Counter);
        var child = scope.ResolveOperator(ChildOperator);
        return Metric == OperatorCountMetric.Invocations
            ? operation.Wrap(child, new InvocationCountingWrapper(counter))
            : operation.Wrap(child, new CandidateCountingWrapper(counter));
    }
}

public static class CountedOperatorExtensions
{
    extension<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source)
        where TOperation : IOperation<TOperation>
    {
        /// <summary>Counts this operator's successful calls, or their candidate work, into the counter.</summary>
        public Counted<TOperation, TCandidate> Counted(Counter counter, OperatorCountMetric metric = OperatorCountMetric.Invocations) => new(source, counter, metric);
    }

    extension<TRoot>(TRoot root) where TRoot : class, IConfigurationNode
    {
        /// <summary>Counts the operator's successful calls, or their candidate work, wherever this configuration holds it.</summary>
        /// <param name="target">The operator to count, matched by reference. It is an error when this configuration does not hold it.</param>
        /// <param name="counter">Receives the new counter, which <c>run.Result(counter).CurrentCount</c> reads and a terminator can check.</param>
        /// <param name="metric">Whether to count successful calls or their candidate work.</param>
        public TRoot Counting<TOperation, TCandidate>(IOperator<TOperation, TCandidate> target, out Counter counter, OperatorCountMetric metric = OperatorCountMetric.Invocations)
            where TOperation : IOperation<TOperation> =>
            root.Substitute(Substitution.Counting(target, out counter, metric));

        /// <summary>Counts the operator's successful calls, or their candidate work, into an existing counter wherever this configuration holds it.</summary>
        /// <remarks>Counting the same target twice into one counter places two wrappers, which count every call twice.</remarks>
        /// <param name="target">The operator to count, matched by reference. It is an error when this configuration does not hold it.</param>
        /// <param name="counter">The counter to add to, such as the one <c>LimitedToCount</c> hands to its placement.</param>
        /// <param name="metric">Whether to count successful calls or their candidate work.</param>
        public TRoot Counting<TOperation, TCandidate>(IOperator<TOperation, TCandidate> target, Counter counter, OperatorCountMetric metric = OperatorCountMetric.Invocations)
            where TOperation : IOperation<TOperation> =>
            root.Substitute(Substitution.Counting(target, counter, metric));
    }

    extension(Substitution)
    {
        /// <summary>The substitution behind <c>Counting</c>, for one call with others: <c>ga.Substitute(Substitution.Counting(ga.Mutator, out var mutations), Substitution.Counting(ga.Evaluator, out var evaluations))</c>.</summary>
        /// <remarks>One call resolves every target against the graph it received, so a target stays reachable although an earlier substitution of the call changed what is below it.</remarks>
        /// <param name="target">The operator to count, matched by reference. It is an error when the configuration does not hold it.</param>
        /// <param name="counter">Receives the new counter.</param>
        /// <param name="metric">Whether to count successful calls or their candidate work.</param>
        public static Substitution Counting<TOperation, TCandidate>(IOperator<TOperation, TCandidate> target, out Counter counter, OperatorCountMetric metric = OperatorCountMetric.Invocations)
            where TOperation : IOperation<TOperation>
        {
            var created = new Counter();
            counter = created;
            return Substitution.Counting(target, created, metric);
        }

        /// <summary>The substitution behind <c>Counting</c> into an existing counter, for one call with others.</summary>
        /// <param name="target">The operator to count, matched by reference. It is an error when the configuration does not hold it.</param>
        /// <param name="counter">The counter to add to.</param>
        /// <param name="metric">Whether to count successful calls or their candidate work.</param>
        public static Substitution Counting<TOperation, TCandidate>(IOperator<TOperation, TCandidate> target, Counter counter, OperatorCountMetric metric = OperatorCountMetric.Invocations)
            where TOperation : IOperation<TOperation> =>
            Substitution.Of(target, node => node.Counted(counter, metric));
    }
}

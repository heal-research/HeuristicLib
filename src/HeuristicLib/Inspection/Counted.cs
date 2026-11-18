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
        var operation = OperationInfo.For<TOperation, TCandidate, TSearchState>();
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
            where TOperation : IOperation<TOperation>
        {
            var created = new Counter();
            counter = created;
            return root.Substitute(Substitution.Of(target, node => node.Counted(created, metric)));
        }
    }
}

/// <summary>Counts each successful invocation.</summary>
internal readonly struct InvocationCountingWrapper(CountAccumulator counter) : IInvocationWrapper
{
    public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
    {
        var result = invocation.Proceed();
        counter.IncrementBy(1);
        return result;
    }
}

/// <summary>Counts the candidate work of each successful invocation.</summary>
internal readonly struct CandidateCountingWrapper(CountAccumulator counter) : IInvocationWrapper
{
    public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
    {
        var result = invocation.Proceed();
        counter.IncrementBy(invocation.CountCandidateWork(result));
        return result;
    }
}

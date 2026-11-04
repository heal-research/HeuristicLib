using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Counts the successful calls of its child, or their candidate work, into the run's result of a counter.</summary>
/// <remarks>
/// This is the explicit placement of an operator counter: it counts what the counter's advice counts, through the same
/// wrapper, but at the configuration it wraps rather than at the counter's source. The run has one result per counter,
/// so a terminator reading the counter sees these calls wherever the wrapper is placed. A candidate counter on an
/// operation without candidate work fails when this configuration is built.
/// </remarks>
public sealed record Counted<TOperation, TCandidate> : WrappingOperator<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
{
    public Counted(IOperator<TOperation, TCandidate> childOperator, OperatorCounter counter) : base(childOperator)
    {
        Counter = counter;
    }

    public OperatorCounter Counter { get; init; }

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        ExplicitPlacement.Apply(scope, ChildOperator, new CountingOperatorAdvice<TOperation, TCandidate>(scope.Results.Result(Counter), Counter.Metric));
}

public static class CountedOperatorExtensions
{
    extension<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source)
        where TOperation : IOperation<TOperation>
    {
        /// <summary>Counts this operator's successful calls, or their candidate work, into the run's result of the counter.</summary>
        public Counted<TOperation, TCandidate> Counted(OperatorCounter counter) => new(source, counter);
    }
}

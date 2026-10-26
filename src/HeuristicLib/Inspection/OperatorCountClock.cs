using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>
/// Counts the successful invocations or the candidate work of one chosen operator.
/// </summary>
/// <remarks>
/// Candidate work, such as the candidates an evaluator evaluated, is the axis that makes runs of different algorithms
/// comparable. Several operators may count different work, which is why the operator is named rather than inferred. A
/// candidate clock over an operation that reports no candidate work fails before its executor is built.
/// </remarks>
public sealed class OperatorCountClock<TOperation, TCandidate>
    : Clock<long>
    where TOperation : IOperation<TOperation>
{
    private readonly CountAccumulator counter = new();

    public OperatorCountClock(IOperator<TOperation, TCandidate> source, OperatorCountMetric metric)
    {
        Advices = [(Pointcut.Reference(source), new CountingOperatorAdvice<TOperation, TCandidate>(counter, metric))];
    }

    protected override long ReadTime() => counter.CurrentCount;
}

public static class OperatorClockExtensions
{
    extension(Clock)
    {
        /// <summary>
        /// Creates a clock counting the successful invocations of one chosen operator.
        /// </summary>
        public static OperatorCountClock<TOperation, TCandidate> FromInvocations<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source)
            where TOperation : IOperation<TOperation> =>
            new(source, OperatorCountMetric.Invocations);

        /// <summary>
        /// Creates a clock counting the candidate work of one chosen operator, such as the candidates an evaluator
        /// evaluated.
        /// </summary>
        public static OperatorCountClock<TOperation, TCandidate> FromCandidates<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source)
            where TOperation : IOperation<TOperation> =>
            new(source, OperatorCountMetric.Candidates);
    }
}

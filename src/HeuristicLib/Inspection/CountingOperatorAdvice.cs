using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Counts successful invocations or their candidate work for the selected operation.</summary>
public sealed class CountingOperatorAdvice<TOperation, TCandidate>(CountAccumulator counter, OperatorCountMetric metric)
    : OperatorAdvice<TOperation, TCandidate> where TOperation : IOperation<TOperation>
{
    public CountAccumulator Counter { get; } = counter;
    public OperatorCountMetric Metric { get; } = metric;

    public override void Check<TSearchSpace, TProblem, TSearchState>(OperationInfo operation,
        IOperator<TOperation, TCandidate> source)
    {
        if (Metric == OperatorCountMetric.Candidates)
            operation.RequireCandidateWork(source, "Counting candidates");
    }

    public override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor, OperationInfo operation,
        IOperator<TOperation, TCandidate> source) =>
        Metric == OperatorCountMetric.Invocations
            ? operation.Wrap(executor, new InvocationCounter(Counter))
            : operation.Wrap(executor, new CandidateCounter(Counter));
}

/// <summary>Counts each successful invocation.</summary>
file readonly struct InvocationCounter(CountAccumulator counter) : IInvocationWrapper
{
    public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
    {
        var result = invocation.Proceed();
        counter.IncrementBy(1);
        return result;
    }
}

/// <summary>Counts the candidate work of each successful invocation.</summary>
file readonly struct CandidateCounter(CountAccumulator counter) : IInvocationWrapper
{
    public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
    {
        var result = invocation.Proceed();
        counter.IncrementBy(invocation.CountCandidateWork(result));
        return result;
    }
}

public static class OperatorCounterExtensions
{
    extension(Aspect)
    {
        public static Aspect CountInvocations<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source, CountAccumulator counter)
            where TOperation : IOperation<TOperation> =>
            Aspect.Create(Pointcut.Reference(source), new CountingOperatorAdvice<TOperation, TCandidate>(counter, OperatorCountMetric.Invocations));

        public static Aspect CountCandidates<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source, CountAccumulator counter)
            where TOperation : IOperation<TOperation> =>
            Aspect.Create(Pointcut.Reference(source), new CountingOperatorAdvice<TOperation, TCandidate>(counter, OperatorCountMetric.Candidates));

        public static Aspect CountCandidates<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source, out CountAccumulator counter)
            where TOperation : IOperation<TOperation>
        {
            counter = new CountAccumulator();
            return Aspect.CountCandidates(source, counter);
        }
    }
}

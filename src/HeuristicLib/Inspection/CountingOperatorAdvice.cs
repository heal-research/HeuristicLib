using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

internal sealed class CountingOperatorAdvice(CountAccumulator counter, OperatorCountMetric metric) : OperatorAdvice
{
    public override void Check(OperationInfo operation, IOperator source)
    {
        if (metric == OperatorCountMetric.Candidates)
            operation.RequireCandidateWork(source, "Counting candidates");
    }

    public override IExecutor Wrap(IExecutor executor, OperationInfo operation, IOperator source) =>
        metric == OperatorCountMetric.Invocations
            ? operation.Wrap(executor, new InvocationCounter(counter))
            : operation.Wrap(executor, new CandidateCounter(counter));
}

/// <summary>Counts successful invocations or their candidate work for the selected operation.</summary>
/// <remarks>The supplied runtime accumulator is shared wherever this advice is reused.</remarks>
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

        /// <summary>Creates an aspect with a new accumulator for successful invocations.</summary>
        public static Aspect CountInvocations<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source, out CountAccumulator counter)
            where TOperation : IOperation<TOperation>
        {
            counter = new CountAccumulator();
            return Aspect.CountInvocations(source, counter);
        }

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

public static class OperatorCountingAspectExtensions
{
    extension<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source)
        where TOperation : IOperation<TOperation>
    {
        /// <summary>Creates an aspect that counts successful invocations in the supplied accumulator.</summary>
        public Aspect CountInvocations(CountAccumulator counter) => Aspect.CountInvocations(source, counter);

        /// <summary>Creates an aspect with a new accumulator for successful invocations.</summary>
        public Aspect CountInvocations(out CountAccumulator counter) => Aspect.CountInvocations(source, out counter);

        /// <summary>Creates an aspect that counts candidate work in the supplied accumulator.</summary>
        public Aspect CountCandidates(CountAccumulator counter) => Aspect.CountCandidates(source, counter);

        /// <summary>Creates an aspect with a new accumulator for candidate work.</summary>
        public Aspect CountCandidates(out CountAccumulator counter) => Aspect.CountCandidates(source, out counter);
    }
}

using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Counts successful calls or candidate work for the selected operation.</summary>
public sealed class CountingOperatorAdvice<TOperation, TCandidate>(CountAccumulator counter, OperatorCountMetric metric)
    : OperatorAdvice<TOperation, TCandidate> where TOperation : IOperationDescriptor<TOperation>
{
    public CountAccumulator Counter { get; } = counter;
    public OperatorCountMetric Metric { get; } = metric;

    public override OperatorAdviceWrapperFactory<TOperation, TCandidate> CreateWrapperFactory(IOperator<TOperation, TCandidate> source) =>
        new Factory(Counter, Metric);

    private sealed class Factory(CountAccumulator counter, OperatorCountMetric metric) : OperatorAdviceWrapperFactory<TOperation, TCandidate>
    {
        private readonly CountingInvocationWrapper invocationWrapper = new(counter, metric);

        public override WrapperExecutorFactory GetWrapperFactory<TSearchSpace, TProblem, TRunSearchState>(
            IOperator<TOperation, TCandidate> source,
            OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState> adapter)
        {
            if (metric == OperatorCountMetric.Candidates)
                adapter.RequireCandidateWork(source, "CountCandidates");
            return adapter.WrapInvocation(invocationWrapper);
        }
    }
}

/// <summary>Counts successful child invocations or the candidate work those invocations performed.</summary>
internal sealed class CountingInvocationWrapper(CountAccumulator counter, OperatorCountMetric metric) : IInvocationWrapper
{
    public Func<TArguments, TResult> Wrap<TArguments, TResult>(Func<TArguments, TResult> invokeChild,
        Func<TArguments, TResult, int>? countCandidateWork)
    {
        if (metric == OperatorCountMetric.Calls)
            return arguments =>
            {
                var result = invokeChild(arguments);
                counter.IncrementBy(1);
                return result;
            };

        var count = countCandidateWork ?? throw new InvalidOperationException("The operation supplies no candidate-work counting function.");
        return arguments =>
        {
            var result = invokeChild(arguments);
            counter.IncrementBy(count(arguments, result));
            return result;
        };
    }
}

public sealed record OperatorCounting<TOperation, TCandidate>(OperatorCountMetric Metric) : IOperatorCounting<IOperator<TOperation, TCandidate>>
    where TOperation : IOperationDescriptor<TOperation>
{
    public Advice<IOperator<TOperation, TCandidate>> CreateAdvice(CountAccumulator counter) =>
        new CountingOperatorAdvice<TOperation, TCandidate>(counter, Metric);
}

public static class OperatorCounterExtensions
{
    extension(Aspect)
    {
        public static Aspect CountCalls<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source, CountAccumulator counter)
            where TOperation : IOperationDescriptor<TOperation> =>
            Aspect.Create(Pointcut.Reference(source), new CountingOperatorAdvice<TOperation, TCandidate>(counter, OperatorCountMetric.Calls));

        public static Aspect CountCandidates<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source, CountAccumulator counter)
            where TOperation : IOperationDescriptor<TOperation> =>
            Aspect.Create(Pointcut.Reference(source), new CountingOperatorAdvice<TOperation, TCandidate>(counter, OperatorCountMetric.Candidates));

        public static Aspect CountCandidates<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source, out CountAccumulator counter)
            where TOperation : IOperationDescriptor<TOperation>
        {
            counter = new CountAccumulator();
            return Aspect.CountCandidates(source, counter);
        }
    }
}

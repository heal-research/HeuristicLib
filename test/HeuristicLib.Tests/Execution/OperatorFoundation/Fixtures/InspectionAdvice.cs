using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures;

public sealed class CallCountingAdvice(CountAccumulator counter) : OperatorAdvice
{
    public override IExecutor Wrap(IExecutor executor, OperationExecutorAdapter operation, IOperator source) =>
        operation.WrapInvocation(executor, new CallCounting(counter));

    private readonly struct CallCounting(CountAccumulator counter) : IInvocationWrapper
    {
        public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
        {
            var result = invocation.Proceed();
            counter.IncrementBy(1);
            return result;
        }
    }
}

public sealed class DurationAdvice(DurationAccumulator duration, TimeProvider time) : OperatorAdvice
{
    public override IExecutor Wrap(IExecutor executor, OperationExecutorAdapter operation, IOperator source) =>
        operation.WrapInvocation(executor, new MeasureDuration(duration, time));

    private readonly struct MeasureDuration(DurationAccumulator duration, TimeProvider time) : IInvocationWrapper
    {
        public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
        {
            var start = time.GetTimestamp();
            try
            {
                return invocation.Proceed();
            }
            finally
            {
                duration.AddDuration(time.GetElapsedTime(start));
            }
        }
    }
}

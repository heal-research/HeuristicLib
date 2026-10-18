using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures;

public sealed class CallCountingAdvice(CountAccumulator counter) : OperatorAdvice
{
    public override OperatorAdviceWrapperFactory CreateWrapperFactory(IOperator source) => new WrapperFactory(counter);

    private sealed class WrapperFactory(CountAccumulator counter) : OperatorAdviceWrapperFactory, IInvocationWrapper
    {
        public override WrapperExecutorFactory GetWrapperFactory<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(
            IOperator<TOperation, TCandidate> source,
            OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState> adapter) => adapter.WrapInvocation(this);

        public Func<TArguments, TResult> Wrap<TArguments, TResult>(Func<TArguments, TResult> invokeChild,
            Func<TArguments, TResult, int>? countCandidateWork) => arguments =>
        {
            var result = invokeChild(arguments);
            counter.IncrementBy(1);
            return result;
        };
    }
}

public sealed class DurationAdvice(DurationAccumulator duration, TimeProvider time) : OperatorAdvice
{
    public override OperatorAdviceWrapperFactory CreateWrapperFactory(IOperator source) => new WrapperFactory(duration, time);

    private sealed class WrapperFactory(DurationAccumulator duration, TimeProvider time) : OperatorAdviceWrapperFactory
    {
        public override WrapperExecutorFactory GetWrapperFactory<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(
            IOperator<TOperation, TCandidate> source,
            OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState> adapter) =>
            adapter.WrapInvocation(new CallbackInvocationWrapper<DurationHooks, long>(new DurationHooks(duration, time)));
    }

    private readonly struct DurationHooks(DurationAccumulator duration, TimeProvider time) : IInvocationCallbacks<long>
    {
        public long OnStarting() => time.GetTimestamp();
        public void OnSucceeded(long token) { }
        public void OnFailed(long token, Exception failure) { }
        public void OnFinished(long token)
        {
            duration.AddDuration(time.GetElapsedTime(token));
        }
    }
}

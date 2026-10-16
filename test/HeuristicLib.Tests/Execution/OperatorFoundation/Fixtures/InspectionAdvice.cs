using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures;

public sealed class CallCountingAdvice(CountAccumulator counter) : OperatorAdvice
{
    public override OperatorAdviceWrapperFactory CreateWrapperFactory(IOperator source) => new WrapperFactory(counter);

    private sealed class WrapperFactory(CountAccumulator counter) : OperatorAdviceWrapperFactory
    {
        public override WrapperExecutorFactory GetWrapperFactory<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
            IOperator<TCandidate, TOperation> source,
            OperationExecutorAdapter<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> adapter) =>
            adapter.Success(new CountSuccess(counter));
    }

    private readonly struct CountSuccess(CountAccumulator counter) : IOperationSuccessCallback
    {
        public void OnSucceeded(int candidateWork)
        {
            counter.IncrementBy(1);
        }
    }
}

public sealed class DurationAdvice(DurationAccumulator duration, TimeProvider time) : OperatorAdvice
{
    public override OperatorAdviceWrapperFactory CreateWrapperFactory(IOperator source) => new WrapperFactory(duration, time);

    private sealed class WrapperFactory(DurationAccumulator duration, TimeProvider time) : OperatorAdviceWrapperFactory
    {
        public override WrapperExecutorFactory GetWrapperFactory<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
            IOperator<TCandidate, TOperation> source,
            OperationExecutorAdapter<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> adapter) =>
            adapter.Hooks<DurationHooks, long>(new DurationHooks(duration, time));
    }

    private readonly struct DurationHooks(DurationAccumulator duration, TimeProvider time) : IOperationCallCallbacks<long>
    {
        public long OnStarting() => time.GetTimestamp();
        public void OnSucceeded(long token, int candidateWork) { }
        public void OnFailed(long token, Exception failure) { }
        public void OnFinished(long token)
        {
            duration.AddDuration(time.GetElapsedTime(token));
        }
    }
}

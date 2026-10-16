using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.OperatorRolePrototype;

public sealed class CallCountingAdvice(CountAccumulator counter) : OperatorAdvice
{
    public override PreparedOperatorAdvice Prepare(IOperator source) => new Prepared(counter);

    private sealed class Prepared(CountAccumulator counter) : PreparedOperatorAdvice
    {
        public override PreparedWrapper Bind<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
            IOperator<TCandidate, TOperation> source,
            OperationBinding<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> binding) =>
            binding.Success(new CountSuccess(counter));
    }

    private readonly struct CountSuccess(CountAccumulator counter) : ISuccessHook
    {
        public void Success(int candidateWork)
        {
            counter.IncrementBy(1);
        }
    }
}

public sealed class DurationAdvice(DurationAccumulator duration, TimeProvider time) : OperatorAdvice
{
    public override PreparedOperatorAdvice Prepare(IOperator source) => new Prepared(duration, time);

    private sealed class Prepared(DurationAccumulator duration, TimeProvider time) : PreparedOperatorAdvice
    {
        public override PreparedWrapper Bind<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
            IOperator<TCandidate, TOperation> source,
            OperationBinding<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> binding) =>
            binding.Hooks<DurationHooks, long>(new DurationHooks(duration, time));
    }

    private readonly struct DurationHooks(DurationAccumulator duration, TimeProvider time) : ICallHooks<long>
    {
        public long Enter() => time.GetTimestamp();
        public void Success(long token, int candidateWork) { }
        public void Fault(long token, Exception failure) { }
        public void Finally(long token)
        {
            duration.AddDuration(time.GetElapsedTime(token));
        }
    }
}

using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

public sealed class CallCounter
{
    public int Value { get; set; }
}

// Diagnostic/failure injection for fixtures. Weak records must not change advice/owner lifetime behavior.
public sealed class AdviceLog
{
    public int Checks { get; private set; }
    public int Wraps { get; private set; }
    public bool FailCheck { get; set; }
    public bool FailWrap { get; set; }
    public Action? AfterSuccess { get; set; }
    public List<WeakReference> CheckedSources { get; } = [];
    public List<WeakReference> WrappedSources { get; } = [];

    public void Check(IOperator source)
    {
        Checks++;
        CheckedSources.Add(new WeakReference(source));
        if (FailCheck)
            throw new InvalidOperationException("Requested advice check failure.");
    }

    public void Wrap(IOperator source)
    {
        Wraps++;
        WrappedSources.Add(new WeakReference(source));
        if (FailWrap)
            throw new InvalidOperationException("Requested advice wrapping failure.");
    }
}

public readonly struct CountingWrapper(CallCounter total, AdviceLog log) : IInvocationWrapper
{
    public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
    {
        var result = invocation.Proceed();
        total.Value++;
        log.AfterSuccess?.Invoke();
        return result;
    }
}

// One external general counter, with no enumeration of operations or execution signatures.
public sealed class CountCalls(CallCounter total, AdviceLog log) : OperatorAdvice
{
    public override void Check(OperationExecutorAdapter operation, IOperator source) => log.Check(source);

    public override IExecutor Wrap(IExecutor executor, OperationExecutorAdapter operation, IOperator source)
    {
        log.Wrap(source);
        return operation.WrapInvocation(executor, new CountingWrapper(total, log));
    }
}

public sealed class CountCalls<TOperation, TCandidate>(CallCounter total, AdviceLog log)
    : OperatorAdvice<TOperation, TCandidate> where TOperation : IOperationDescriptor<TOperation>
{
    public override void Check<TSearchSpace, TProblem, TSearchState>(OperationExecutorAdapter operation,
        IOperator<TOperation, TCandidate> source) => log.Check(source);

    public override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor, OperationExecutorAdapter operation,
        IOperator<TOperation, TCandidate> source)
    {
        log.Wrap(source);
        return operation.WrapInvocation(executor, new CountingWrapper(total, log));
    }
}

public sealed class WrongViewAdvice(CallCounter total, AdviceLog log) : OperatorAdvice<Mutation, RealVector>
{
    public bool ReturnWrongView { get; set; } = true;

    public override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor, OperationExecutorAdapter operation,
        IOperator<Mutation, RealVector> source)
    {
        log.Wrap(source);
        return ReturnWrongView ? new WrongExecutor() : operation.WrapInvocation(executor, new CountingWrapper(total, log));
    }

    private sealed class WrongExecutor : IChecksumExecutor
    {
        public int Read() => 0;
    }
}

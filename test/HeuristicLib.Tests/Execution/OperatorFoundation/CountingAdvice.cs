using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

public sealed class CallCounter
{
    public int Value { get; set; }
}

public sealed class CallCountState
{
    public int Calls { get; set; }
    public int LastCandidateWork { get; set; }
}

// Diagnostic/failure injection for fixtures. Weak records must not change advice/owner lifetime behavior.
public sealed class AdviceLog
{
    public int Preparations { get; private set; }
    public int Bindings { get; private set; }
    public bool FailPreparation { get; set; }
    public bool FailBinding { get; set; }
    public Action? AfterSuccess { get; set; }
    public List<WeakReference> PreparedSources { get; } = [];
    public List<WeakReference> BoundSources { get; } = [];
    public List<WeakReference> States { get; } = [];

    public CallCountState Prepare(IOperator source)
    {
        Preparations++;
        if (FailPreparation)
            throw new InvalidOperationException("Requested advice preparation failure.");
        var state = new CallCountState();
        PreparedSources.Add(new WeakReference(source));
        States.Add(new WeakReference(state));
        return state;
    }

    public void Bind(IOperator source)
    {
        Bindings++;
        BoundSources.Add(new WeakReference(source));
        if (FailBinding)
            throw new InvalidOperationException("Requested advice binding failure.");
    }

    public CallCountState StateAt(int index) => (CallCountState)States[index].Target!;
}

public readonly struct CountHook(CallCounter total, CallCountState state, AdviceLog log) : IOperationSuccessCallback
{
    public void OnSucceeded(int candidateWork)
    {
        state.Calls++;
        state.LastCandidateWork = candidateWork;
        total.Value++;
        log.AfterSuccess?.Invoke();
    }
}

// One external general counter, with no enumeration of operations or execution signatures.
public sealed class CountCalls(CallCounter total, AdviceLog log) : OperatorAdvice
{
    public override OperatorAdviceWrapperFactory CreateWrapperFactory(IOperator source) => new WrapperFactory(this, log.Prepare(source));

    // Deliberately retains its advice key: the weak-cache test must handle a key/value reference cycle.
    private sealed class WrapperFactory(CountCalls advice, CallCountState state) : OperatorAdviceWrapperFactory
    {
        public override WrapperExecutorFactory GetWrapperFactory<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(
            IOperator<TOperation, TCandidate> source,
            OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState> adapter)
        {
            advice.RecordBinding(source);
            return adapter.Success(advice.CreateHook(state));
        }
    }

    private void RecordBinding(IOperator source)
    {
        log.Bind(source);
    }
    private CountHook CreateHook(CallCountState state) => new(total, state, log);
}

public sealed class CountCalls<TOperation, TCandidate>(CallCounter total, AdviceLog log)
    : OperatorAdvice<TOperation, TCandidate> where TOperation : IOperationDescriptor<TOperation>
{
    public override OperatorAdviceWrapperFactory<TOperation, TCandidate> CreateWrapperFactory(IOperator<TOperation, TCandidate> source) =>
        new WrapperFactory(this, log.Prepare(source));

    private sealed class WrapperFactory(CountCalls<TOperation, TCandidate> advice, CallCountState state)
        : OperatorAdviceWrapperFactory<TOperation, TCandidate>
    {
        public override WrapperExecutorFactory GetWrapperFactory<TSearchSpace, TProblem, TRunSearchState>(
            IOperator<TOperation, TCandidate> source,
            OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState> adapter)
        {
            advice.RecordBinding(source);
            return adapter.Success(advice.CreateHook(state));
        }
    }

    private void RecordBinding(IOperator source)
    {
        log.Bind(source);
    }
    private CountHook CreateHook(CallCountState state) => new(total, state, log);
}

public sealed class WrongViewAdvice(CallCounter total, AdviceLog log) : OperatorAdvice<Mutation, RealVector>
{
    public bool ReturnWrongView { get; set; } = true;

    public override OperatorAdviceWrapperFactory<Mutation, RealVector> CreateWrapperFactory(
        IOperator<Mutation, RealVector> source) =>
        new WrapperFactory(this, log.Prepare(source));

    private sealed class WrapperFactory(WrongViewAdvice advice, CallCountState state)
        : OperatorAdviceWrapperFactory<Mutation, RealVector>
    {
        public override WrapperExecutorFactory GetWrapperFactory<TSearchSpace, TProblem, TRunSearchState>(
            IOperator<Mutation, RealVector> source,
            OperationExecutorAdapter<Mutation, RealVector, TSearchSpace, TProblem, TRunSearchState> adapter) =>
            advice.BindView(source, adapter.Success(new CountHook(advice.Total, state, advice.Log)));
    }

    private CallCounter Total => total;
    private AdviceLog Log => log;

    private WrapperExecutorFactory BindView(IOperator source, WrapperExecutorFactory valid)
    {
        log.Bind(source);
        return ReturnWrongView ? WrapperExecutorFactory.From<IChecksumExecutor>(static child => child) : valid;
    }
}

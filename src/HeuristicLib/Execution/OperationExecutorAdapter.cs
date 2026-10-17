using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Connects an operation's executor interface to shared forwarding and advice code.</summary>
/// <remarks>
/// Implements how the operation is wrapped and how successful calls report candidate work. For example, mutation's
/// adapter calls Mutate and reports the returned candidate count. External operation descriptors supply their own adapter.
/// </remarks>
public abstract class OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState>
    where TOperation : IOperationDescriptor<TOperation>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TRunSearchState : class, ISearchState
{
    /// <summary>Rejects unsupported candidate metrics before an executor or its children are constructed.</summary>
    public void RequireCandidateWork(IOperator<TOperation, TCandidate> source, string metric)
    {
        if (!SupportsCandidateWork)
            throw new InvalidOperationException($"{metric} cannot measure candidate work for {typeof(TOperation).Name} on {source.GetType().Name}.");
    }

    public abstract Type ExecutorType { get; }
    /// <summary>Whether success callbacks receive a meaningful candidate-work count for this operation.</summary>
    /// <remarks>Metric advice must check applicability during binding, before constructing the execution graph.</remarks>
    public abstract bool SupportsCandidateWork { get; }
    public abstract WrapperExecutorFactory Identity();
    public abstract ExecutorFactory Wrap(IOperator<TOperation, TCandidate> child, WrapperExecutorFactory wrapper);
    public abstract WrapperExecutorFactory Success<TCallback>(TCallback callback) where TCallback : struct, IOperationSuccessCallback;
    public abstract WrapperExecutorFactory Hooks<TCallbacks, TCallData>(TCallbacks callbacks) where TCallbacks : struct, IOperationCallCallbacks<TCallData>;
    public abstract WrapperExecutorFactory PolicyWrapper(SynchronousPolicy policy);

    public ExecutorFactory Forward(IOperator<TOperation, TCandidate> child) => Wrap(child, Identity());
}

/// <summary>Implements shared wrapping and child resolution for an operation whose executor interface is TExecutor.</summary>
/// <remarks>Derived adapters implement the operation's named method and notify the supplied callbacks.</remarks>
public abstract class OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState, TExecutor>
    : OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState>
    where TOperation : IOperationDescriptor<TOperation>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TRunSearchState : class, ISearchState
    where TExecutor : class, IExecutor
{
    public sealed override Type ExecutorType => typeof(TExecutor);

    public sealed override WrapperExecutorFactory Identity() => WrapperExecutorFactory.From<TExecutor>(static child => child);

    public sealed override WrapperExecutorFactory Success<TCallback>(TCallback callback) =>
        WrapperExecutorFactory.From<TExecutor>(child =>
            new ExecutorWithControls<TExecutor>(CreateSuccessCallbackExecutor(child.Executor, callback), child.ControlProvider));

    protected abstract TExecutor CreateSuccessCallbackExecutor<TCallback>(TExecutor child, TCallback callback)
        where TCallback : struct, IOperationSuccessCallback;

    public sealed override WrapperExecutorFactory Hooks<TCallbacks, TCallData>(TCallbacks callbacks) =>
        WrapperExecutorFactory.From<TExecutor>(child =>
            new ExecutorWithControls<TExecutor>(CreateCallCallbacksExecutor<TCallbacks, TCallData>(child.Executor, callbacks), child.ControlProvider));

    protected abstract TExecutor CreateCallCallbacksExecutor<TCallbacks, TCallData>(TExecutor child, TCallbacks callbacks)
        where TCallbacks : struct, IOperationCallCallbacks<TCallData>;

    public sealed override WrapperExecutorFactory PolicyWrapper(SynchronousPolicy policy) =>
        WrapperExecutorFactory.From<TExecutor>(child => new ExecutorWithControls<TExecutor>(
            CreatePolicyExecutor([child.Executor], policy), child.ControlProvider));

    protected abstract TExecutor CreatePolicyExecutor(ImmutableArray<TExecutor> children, SynchronousPolicy policy);

    public sealed override ExecutorFactory Wrap(IOperator<TOperation, TCandidate> child, WrapperExecutorFactory wrapper)
    {
        var construct = wrapper.GetFor<TExecutor>();
        return ExecutorFactory.FromFactoryWithControls<TExecutor>(scope => construct(scope.ResolveOperator<TOperation, TCandidate,
            TSearchSpace, TProblem, TRunSearchState, TExecutor>(child, this)));
    }
}

using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Connects an operation's named executor method to forwarding, call wrappers and advice.</summary>
/// <remarks>
/// Implements how the operation is wrapped and supplies its candidate-work counting function. For example, mutation's
/// adapter calls Mutate and counts returned candidates. External operation descriptors supply their own adapter.
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
    /// <summary>Whether the operation supplies a meaningful candidate-work counting function.</summary>
    /// <remarks>Metric advice must check applicability during binding, before constructing the execution graph.</remarks>
    public abstract bool SupportsCandidateWork { get; }
    public abstract WrapperExecutorFactory Identity();
    public abstract ExecutorFactory Wrap(IOperator<TOperation, TCandidate> child, WrapperExecutorFactory wrapper);
    /// <summary>Constructs a named-method executor using the supplied factory to wrap its typed child call.</summary>
    public abstract WrapperExecutorFactory WrapInvocation(IInvocationWrapper invocationWrapper);

    public ExecutorFactory Forward(IOperator<TOperation, TCandidate> child) => Wrap(child, Identity());
}

/// <summary>Implements shared wrapping and child resolution for an operation whose executor interface is TExecutor.</summary>
/// <remarks>Derived adapters connect the named method to a typed child function and candidate-work counting rule.</remarks>
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

    public sealed override WrapperExecutorFactory WrapInvocation(IInvocationWrapper invocationWrapper) =>
        WrapperExecutorFactory.From<TExecutor>(child => new ExecutorWithControls<TExecutor>(
            CreateInvocationWrapperExecutor(child.Executor, invocationWrapper), child.ControlProvider));

    protected abstract TExecutor CreateInvocationWrapperExecutor(TExecutor child, IInvocationWrapper invocationWrapper);

    public sealed override ExecutorFactory Wrap(IOperator<TOperation, TCandidate> child, WrapperExecutorFactory wrapper)
    {
        var construct = wrapper.GetFor<TExecutor>();
        return ExecutorFactory.FromFactoryWithControls<TExecutor>(scope => construct(scope.ResolveOperator<TOperation, TCandidate,
            TSearchSpace, TProblem, TRunSearchState, TExecutor>(child, this)));
    }
}

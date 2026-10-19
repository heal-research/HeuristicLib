using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Connects one operation's named executor method, for one set of run types, to invocation wrappers.</summary>
/// <remarks>
/// An operation descriptor supplies its adapter. For example, mutation's adapter wraps Mutate and counts the returned
/// candidates. Advice receives the adapter to wrap executors without knowing their operation.
/// </remarks>
public abstract class OperationExecutorAdapter
{
    private protected OperationExecutorAdapter() { }

    /// <summary>The executor interface that the operation's executors implement for these run types.</summary>
    public abstract Type ExecutorType { get; }

    /// <summary>Whether the operation's invocations report candidate work.</summary>
    /// <remarks>
    /// Advice measuring candidate work checks this before the executor is built. The default is false; an operation with
    /// candidate work overrides it and implements <see cref="IInvocation{TResult}.CountCandidateWork"/>.
    /// </remarks>
    public virtual bool SupportsCandidateWork => false;

    /// <summary>Rejects an operator whose operation reports no candidate work.</summary>
    public void RequireCandidateWork(IOperator source, string metric)
    {
        if (!SupportsCandidateWork)
            throw new InvalidOperationException($"{metric} cannot measure candidate work for {source.GetType().Name}: {ExecutorType.Name} reports none.");
    }

    /// <summary>Returns an executor whose named method passes each call, as an invocation, to the wrapper.</summary>
    /// <remarks>
    /// The returned executor forwards the controls of the executor it wraps, unless the wrapper implements
    /// <see cref="IExecutorControlProvider"/> and supplies them.
    /// </remarks>
    public abstract IExecutor WrapInvocation<TWrapper>(IExecutor executor, TWrapper wrapper) where TWrapper : struct, IInvocationWrapper;

    internal static OperationExecutorAdapter For<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>()
        where TOperation : IOperationDescriptor<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Cache<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>.Adapter;

    // Adapters are stateless, so one instance serves every resolution with the same operation and run types.
    private static class Cache<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>
        where TOperation : IOperationDescriptor<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        internal static readonly OperationExecutorAdapter Adapter = TOperation.CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>();
    }
}

/// <summary>An operation adapter for executors implementing <typeparamref name="TExecutor"/>.</summary>
/// <remarks>
/// An operation author supplies one wrapping executor: its named method creates a struct invocation holding the wrapped
/// executor and the call's arguments and passes it to the wrapper. The wrapping executor implements
/// <see cref="IExecutorControlProvider"/> through <see cref="ExecutorControls.Find{TControl, TWrapper}"/>.
/// </remarks>
public abstract class OperationExecutorAdapter<TExecutor> : OperationExecutorAdapter
    where TExecutor : class, IExecutor
{
    public sealed override Type ExecutorType => typeof(TExecutor);

    public sealed override IExecutor WrapInvocation<TWrapper>(IExecutor executor, TWrapper wrapper) =>
        CreateWrappedExecutor((TExecutor)executor, wrapper);

    /// <summary>Creates an executor whose named method passes each call, as an invocation, to the wrapper.</summary>
    protected abstract TExecutor CreateWrappedExecutor<TWrapper>(TExecutor executor, TWrapper wrapper)
        where TWrapper : struct, IInvocationWrapper;
}

using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Holds one configured operator's prepared execution data, such as counters and caches, and constructs executors
/// sharing that data.
/// </summary>
/// <remarks>
/// The resolver keeps this object for the configuration reference and calls <see cref="CreateExecutor"/> for every
/// resolution, which can request any operation and run types the operator supports. Resolve children inside that call
/// without resetting the shared data.
/// </remarks>
public abstract class OperatorExecutorFactory
{
    /// <summary>Constructs an executor for the requested operation and run types, resolving children through the scope.</summary>
    /// <remarks>Resolution checks that the result implements the operation's executor interface for those run types.</remarks>
    public abstract IExecutor CreateExecutor<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TOperation : IOperationDescriptor<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState;

    /// <summary>Uses one construction function for every operation and run types its executor type supports.</summary>
    /// <remarks>A request that <typeparamref name="TExecutor"/> cannot serve fails before the function runs.</remarks>
    public static OperatorExecutorFactory FromFactory<TExecutor>(ExecutorFactory<TExecutor> factory)
        where TExecutor : class, IExecutor => new SingleFactory<TExecutor>(factory);

    /// <summary>Returns the child's executor for the same operation and run types, adding no executor of its own.</summary>
    public static OperatorExecutorFactory Forward<TOperation, TCandidate>(IOperator<TOperation, TCandidate> childOperator)
        where TOperation : IOperationDescriptor<TOperation> => new ForwardingFactory<TOperation, TCandidate>(childOperator);

    /// <summary>Resolves the child for the same operation and run types and passes each of its calls to the wrapper.</summary>
    /// <remarks>
    /// The wrapping executor forwards the child's controls, unless the wrapper implements
    /// <see cref="IExecutorControlProvider"/> and supplies them.
    /// </remarks>
    public static OperatorExecutorFactory WrapInvocation<TOperation, TCandidate, TWrapper>(IOperator<TOperation, TCandidate> childOperator, TWrapper wrapper)
        where TOperation : IOperationDescriptor<TOperation>
        where TWrapper : struct, IInvocationWrapper => new InvocationWrappingFactory<TOperation, TCandidate, TWrapper>(childOperator, wrapper);

    private sealed class SingleFactory<TExecutor>(ExecutorFactory<TExecutor> factory) : OperatorExecutorFactory
        where TExecutor : class, IExecutor
    {
        public override IExecutor CreateExecutor<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        {
            var requested = OperationExecutorAdapter.For<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>().ExecutorType;
            if (!typeof(TExecutor).IsAssignableTo(requested))
                throw new InvalidOperationException($"Executor factory for {typeof(TExecutor)} cannot provide {requested}.");
            return factory(scope.Scope);
        }
    }

    private sealed class ForwardingFactory<TOperation, TCandidate>(IOperator<TOperation, TCandidate> child)
        : OperatorExecutorFactory<TOperation, TCandidate>
        where TOperation : IOperationDescriptor<TOperation>
    {
        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) => scope.ResolveOperator(child);
    }

    private sealed class InvocationWrappingFactory<TOperation, TCandidate, TWrapper>(IOperator<TOperation, TCandidate> child, TWrapper wrapper)
        : OperatorExecutorFactory<TOperation, TCandidate>
        where TOperation : IOperationDescriptor<TOperation>
        where TWrapper : struct, IInvocationWrapper
    {
        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
            OperationExecutorAdapter.For<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>()
                .WrapInvocation(scope.ResolveOperator(child), wrapper);
    }
}

/// <summary>Constructs executors of one operation and candidate type, sharing data across run types.</summary>
/// <remarks>Derive from the non-generic base when one configuration supports several operations.</remarks>
public abstract class OperatorExecutorFactory<TOperation, TCandidate> : OperatorExecutorFactory
    where TOperation : IOperationDescriptor<TOperation>
{
    public sealed override IExecutor CreateExecutor<TRequestedOperation, TRequestedCandidate, TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TRequestedCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        if (this is not OperatorExecutorFactory<TRequestedOperation, TRequestedCandidate> typed)
            throw new InvalidOperationException($"{GetType().Name} supports {typeof(TCandidate)} / {typeof(TOperation)}, " +
                $"not {typeof(TRequestedCandidate)} / {typeof(TRequestedOperation)}.");
        return typed.CreateTypedExecutor(scope);
    }

    // Reached through the checked instance above, whose candidate type satisfies the run-type constraints.
    internal IExecutor CreateTypedExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState => CreateExecutor(scope);

    /// <summary>Constructs this operation's executor for the run types, resolving children through the scope.</summary>
    protected abstract IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState;
}

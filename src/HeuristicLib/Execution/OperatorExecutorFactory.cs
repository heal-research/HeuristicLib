using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Supplies construction functions for one configured operator. Those functions share the counters, caches and
/// other execution data created with this factory.
/// </summary>
/// <remarks>
/// The resolver reuses this object when it resolves the same operator again. Obtain a construction function for
/// each supported operation and set of run types; resolve children inside that function without resetting shared data.
/// </remarks>
public abstract class OperatorExecutorFactory
{
    public abstract ExecutorFactory GetExecutorFactory<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState> adapter)
        where TOperation : IOperationDescriptor<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;

    public static OperatorExecutorFactory FromFactory<TExecutor>(ExecutorFactory<TExecutor> factory)
        where TExecutor : class, IExecutor => new SingleFactory(ExecutorFactory.FromFactory(factory));

    private sealed class SingleFactory(ExecutorFactory factory) : OperatorExecutorFactory
    {
        public override ExecutorFactory GetExecutorFactory<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(
            OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState> adapter) => factory;
    }
}

/// <summary>Creates executor factories for one candidate type and operation, sharing data across run signatures.</summary>
/// <remarks>Use the non-generic base when one configuration supports several operations.</remarks>
public abstract class OperatorExecutorFactory<TOperation, TCandidate> : OperatorExecutorFactory
    where TOperation : IOperationDescriptor<TOperation>
{
    public sealed override ExecutorFactory GetExecutorFactory<TBoundOperation, TBoundCandidate, TSearchSpace, TProblem, TRunSearchState>(
        OperationExecutorAdapter<TBoundOperation, TBoundCandidate, TSearchSpace, TProblem, TRunSearchState> adapter)
    {
        if (this is not OperatorExecutorFactory<TBoundOperation, TBoundCandidate> typed)
            throw new InvalidOperationException($"{GetType().Name} supports {typeof(TCandidate)} / {typeof(TOperation)}, " +
                $"not {typeof(TBoundCandidate)} / {typeof(TBoundOperation)}.");
        return typed.GetExecutorFactory<TSearchSpace, TProblem, TRunSearchState>(adapter);
    }

    public abstract ExecutorFactory GetExecutorFactory<TSearchSpace, TProblem, TRunSearchState>(
        OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState> adapter)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;
}

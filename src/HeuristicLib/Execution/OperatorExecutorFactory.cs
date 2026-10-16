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
    public abstract ExecutorFactory GetExecutorFactory<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
        OperationExecutorAdapter<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> adapter)
        where TOperation : IOperationDescriptor<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;

    public static OperatorExecutorFactory FromFactory<TExecutor>(ExecutorFactory<TExecutor> factory)
        where TExecutor : class, IExecutor => new SingleFactory(ExecutorFactory.FromFactory(factory));

    private sealed class SingleFactory(ExecutorFactory factory) : OperatorExecutorFactory
    {
        public override ExecutorFactory GetExecutorFactory<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
            OperationExecutorAdapter<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> adapter) => factory;
    }
}

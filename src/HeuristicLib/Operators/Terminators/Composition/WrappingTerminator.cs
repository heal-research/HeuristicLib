using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

/// <remarks>
/// A wrapping terminator owns a child, so it stays agnostic in the search space, problem and search state, and passes
/// the run's binding through to the child unchanged.
/// </remarks>
public abstract record WrappingTerminator<TCandidate>
    : ITerminator<TCandidate>
{
    protected WrappingTerminator(ITerminator<TCandidate> childTerminator)
    {
        ChildTerminator = childTerminator;
    }

    public ITerminator<TCandidate> ChildTerminator { get; init; }

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits(ChildTerminator);

    /// <summary>Prepares this operator once, then binds its child in each construction scope.</summary>
    public ExecutorFactory<ITerminationExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateExecutorFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        var create = CreateWrapperFactory<TRunSearchSpace, TRunProblem, TRunSearchState>();
        return scope => create(scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>(ChildTerminator));
    }

    /// <summary>Prepares persistent execution data and returns a constructor accepting the resolved child.</summary>
    /// <remarks>Allocate shared state here; construct nodes with their contextual children in the returned delegate.</remarks>
    protected abstract WrapperExecutorFactory<ITerminationExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;

}

public abstract class WrappingTerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator)
    : TerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> ChildTerminator { get; } = childTerminator;
}

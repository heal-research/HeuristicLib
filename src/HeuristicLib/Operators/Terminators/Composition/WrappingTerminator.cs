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


    /// <summary>
    /// Resolves the child over the run's search space and problem and hands it to <see
    /// cref="WrapExecutionInstance{TRunSearchSpace, TRunProblem, TRunSearchState}"/>.
    /// </summary>
    public ITerminatorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ResolutionScope scope)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState =>
        WrapExecutionInstance(scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>(ChildTerminator));

    /// <summary>Wraps the child's execution node in this operator's own.</summary>
    protected abstract ITerminatorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> WrapExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ITerminatorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> childTerminator)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public abstract class WrappingTerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator)
    : TerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState> ChildTerminator { get; } = childTerminator;
}

using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

/// <remarks>
/// A multi terminator owns children, so it stays agnostic in the search space, problem and search state, and passes the
/// run's binding through to the children unchanged.
/// </remarks>
public abstract record MultiTerminator<TCandidate>
    : ITerminator<TCandidate>
{
    protected MultiTerminator(IReadOnlyList<ITerminator<TCandidate>> childTerminators)
    {
        ChildTerminators = childTerminators.ToValueArray();
    }

    public ValueArray<ITerminator<TCandidate>> ChildTerminators { get; init; }

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits([.. ChildTerminators]);


    /// <summary>
    /// Resolves each child over the run's search space and problem and hands them to <see
    /// cref="CombineExecutionInstances{TRunSearchSpace, TRunProblem, TRunSearchState}"/>.
    /// </summary>
    public ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState =>
        CombineExecutionInstances([.. ChildTerminators.Select(child => instanceRegistry.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>(child))]);

    /// <summary>Combines the children's execution instances into this operator's own.</summary>
    protected abstract ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CombineExecutionInstances<TRunSearchSpace, TRunProblem, TRunSearchState>(ImmutableArray<ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> childTerminators)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public abstract class MultiTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(ImmutableArray<ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> childTerminators)
    : TerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> ChildTerminators { get; } = childTerminators;
}

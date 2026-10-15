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

    /// <summary>Prepares this operator once, then binds its children in each construction scope.</summary>
    public ExecutionFactory<ITerminatorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateExecutionFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        var create = CreateCompositeFactory<TRunSearchSpace, TRunProblem, TRunSearchState>();
        return scope => create([.. ChildTerminators.Select(child => scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>(child))]);
    }

    /// <summary>Prepares persistent execution data and returns a constructor accepting the resolved children.</summary>
    /// <remarks>Allocate shared state here; construct nodes with their contextual children in the returned delegate.</remarks>
    protected abstract CompositeExecutionFactory<ITerminatorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateCompositeFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public abstract class MultiTerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>(ImmutableArray<ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>> childTerminators)
    : TerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>> ChildTerminators { get; } = childTerminators;
}

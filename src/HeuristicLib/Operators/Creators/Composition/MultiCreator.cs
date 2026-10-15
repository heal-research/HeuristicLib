using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

/// <remarks>
/// A multi creator owns children, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged.
/// </remarks>
public abstract record MultiCreator<TCandidate>
    : ICreator<TCandidate>
{
    protected MultiCreator(IReadOnlyList<ICreator<TCandidate>> childCreators)
    {
        ChildCreators = childCreators.ToValueArray();
    }

    public ValueArray<ICreator<TCandidate>> ChildCreators { get; init; }

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits([.. ChildCreators]);

    /// <summary>Prepares this operator once, then binds its children in each construction scope.</summary>
    public ExecutionFactory<ICreatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var create = CreateCompositeFactory<TRunSearchSpace, TRunProblem>();
        return scope =>
        {
            var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem>();
            return create([.. ChildCreators.Select(child => typed.Resolve(child))]);
        };
    }

    /// <summary>Prepares persistent execution data and returns a constructor accepting the resolved children.</summary>
    /// <remarks>Allocate shared state here; construct nodes with their contextual children in the returned delegate.</remarks>
    protected abstract CompositeExecutionFactory<ICreatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateCompositeFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class MultiCreatorExecution<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ICreatorExecution<TCandidate, TSearchSpace, TProblem>> childCreators)
    : CreatorExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ICreatorExecution<TCandidate, TSearchSpace, TProblem>> ChildCreators { get; } = childCreators;
}

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


    /// <summary>
    /// Resolves each child over the run's search space and problem and hands them to
    /// <see cref="CombineExecutionInstances{TRunSearchSpace, TRunProblem}"/>.
    /// </summary>
    public ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return CombineExecutionInstances([.. ChildCreators.Select(child => resolver.Resolve(child))]);
    }

    /// <summary>Combines the children's execution instances into this operator's own.</summary>
    protected abstract ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem>> childCreators)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class MultiCreatorInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> childCreators)
    : CreatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> ChildCreators { get; } = childCreators;
}

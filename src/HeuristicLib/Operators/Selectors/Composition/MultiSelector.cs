using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

/// <remarks>
/// A multi selector owns children, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged.
/// </remarks>
public abstract record MultiSelector<TCandidate>
    : ISelector<TCandidate>
{
    protected MultiSelector(IReadOnlyList<ISelector<TCandidate>> childSelectors)
    {
        ChildSelectors = childSelectors.ToValueArray();
    }

    public ValueArray<ISelector<TCandidate>> ChildSelectors { get; init; }

    /// <summary>
    /// Resolves each child over the run's search space and problem and hands them to <see
    /// cref="CombineExecutionInstances{TRunSearchSpace, TRunProblem}"/>.
    /// </summary>
    public ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return CombineExecutionInstances([.. ChildSelectors.Select(child => resolver.Resolve(child))]);
    }

    /// <summary>Combines the children's execution instances into this operator's own.</summary>
    protected abstract ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem>> childSelectors)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class MultiSelectorInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> childSelectors)
    : SelectorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> ChildSelectors { get; } = childSelectors;
}

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

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits([.. ChildSelectors]);


    /// <summary>
    /// Resolves each child over the run's search space and problem and hands them to <see
    /// cref="CombineExecutionInstances{TRunSearchSpace, TRunProblem}"/>.
    /// </summary>
    public ISelectorExecution<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return CombineExecutionInstances([.. ChildSelectors.Select(child => typed.Resolve(child))]);
    }

    /// <summary>Combines the children's execution nodes into this operator's own.</summary>
    protected abstract ISelectorExecution<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<ISelectorExecution<TCandidate, TRunSearchSpace, TRunProblem>> childSelectors)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class MultiSelectorExecution<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ISelectorExecution<TCandidate, TSearchSpace, TProblem>> childSelectors)
    : SelectorExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ISelectorExecution<TCandidate, TSearchSpace, TProblem>> ChildSelectors { get; } = childSelectors;
}

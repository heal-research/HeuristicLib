using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

/// <remarks>
/// A wrapping selector owns a child, so it stays agnostic in the search space and problem and passes the run's
/// triple through unchanged.
/// </remarks>
public abstract record WrappingSelector<TCandidate>
    : ISelector<TCandidate>
{
    protected WrappingSelector(ISelector<TCandidate> childSelector)
    {
        ChildSelector = childSelector;
    }

    public ISelector<TCandidate> ChildSelector { get; init; }

    /// <summary>
    /// Resolves the child over the run's search space and problem and hands it to
    /// <see cref="WrapExecutionInstance{TRunSearchSpace, TRunProblem}"/>.
    /// </summary>
    public ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace> =>
        WrapExecutionInstance(instanceRegistry.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildSelector));

    /// <summary>Wraps the child's execution instance in this operator's own.</summary>
    protected abstract ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> childSelector)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class WrappingSelectorInstance<TCandidate, TSearchSpace, TProblem>(ISelectorInstance<TCandidate, TSearchSpace, TProblem> childSelector)
    : SelectorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ISelectorInstance<TCandidate, TSearchSpace, TProblem> ChildSelector { get; } = childSelector;
}

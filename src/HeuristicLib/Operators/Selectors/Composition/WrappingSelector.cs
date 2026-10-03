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

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits(ChildSelector);

    /// <summary>Prepares this operator once, then binds its child in each construction scope.</summary>
    public ExecutionFactory<ISelectorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var create = CreateWrapperFactory<TRunSearchSpace, TRunProblem>();
        return scope => create(scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildSelector));
    }

    /// <summary>Prepares persistent execution data and returns a constructor accepting the resolved child.</summary>
    /// <remarks>Allocate shared state here; construct nodes with their contextual children in the returned delegate.</remarks>
    protected abstract WrapperExecutionFactory<ISelectorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class WrappingSelectorExecution<TCandidate, TSearchSpace, TProblem>(ISelectorExecution<TCandidate, TSearchSpace, TProblem> childSelector)
    : SelectorExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ISelectorExecution<TCandidate, TSearchSpace, TProblem> ChildSelector { get; } = childSelector;
}

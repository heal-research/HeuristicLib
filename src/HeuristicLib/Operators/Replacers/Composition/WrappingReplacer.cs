using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

/// <remarks>
/// A wrapping replacer owns a child, so it stays agnostic in the search space and problem and passes the run's
/// triple through unchanged.
/// </remarks>
public abstract record WrappingReplacer<TCandidate>
    : IReplacer<TCandidate>
{
    protected WrappingReplacer(IReplacer<TCandidate> childReplacer)
    {
        ChildReplacer = childReplacer;
    }

    public IReplacer<TCandidate> ChildReplacer { get; init; }

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits(ChildReplacer);

    /// <summary>Prepares this operator once, then binds its child in each construction scope.</summary>
    public ExecutorFactory<IReplacementExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var create = CreateWrapperFactory<TRunSearchSpace, TRunProblem>();
        return scope => create(scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildReplacer));
    }

    /// <summary>Prepares persistent execution data and returns a constructor accepting the resolved child.</summary>
    /// <remarks>Allocate shared state here; construct nodes with their contextual children in the returned delegate.</remarks>
    protected abstract WrapperExecutorFactory<IReplacementExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;

}

public abstract class WrappingReplacementExecutor<TCandidate, TSearchSpace, TProblem>(IReplacementExecutor<TCandidate, TSearchSpace, TProblem> childReplacer)
    : ReplacementExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IReplacementExecutor<TCandidate, TSearchSpace, TProblem> ChildReplacer { get; } = childReplacer;
}

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


    /// <summary>
    /// Resolves the child over the run's search space and problem and hands it to
    /// <see cref="WrapExecutionInstance{TRunSearchSpace, TRunProblem}"/>.
    /// </summary>
    public IReplacerExecution<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace> =>
        WrapExecutionInstance(scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildReplacer));

    /// <summary>Wraps the child's execution node in this operator's own.</summary>
    protected abstract IReplacerExecution<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IReplacerExecution<TCandidate, TRunSearchSpace, TRunProblem> childReplacer)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class WrappingReplacerExecution<TCandidate, TSearchSpace, TProblem>(IReplacerExecution<TCandidate, TSearchSpace, TProblem> childReplacer)
    : ReplacerExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IReplacerExecution<TCandidate, TSearchSpace, TProblem> ChildReplacer { get; } = childReplacer;
}

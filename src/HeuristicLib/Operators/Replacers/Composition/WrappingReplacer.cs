using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

/// <remarks>
/// A wrapping replacer owns a child, so it stays agnostic in the search space and problem and passes the run's
/// triple through unchanged. Binding is a leaf concept: a composite that narrowed the triple would reject children the
/// run supports.
/// </remarks>
public abstract record WrappingReplacer<TCandidate>
    : IReplacer<TCandidate>
{
    protected WrappingReplacer(IReplacer<TCandidate> childReplacer)
    {
        ChildReplacer = childReplacer;
    }

    public IReplacer<TCandidate> ChildReplacer { get; init; }

    /// <summary>
    /// Resolves the child over the run's search space and problem and hands it to
    /// <see cref="WrapExecutionInstance{TRunSearchSpace, TRunProblem}"/>.
    /// </summary>
    public IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace> =>
        WrapExecutionInstance(instanceRegistry.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildReplacer));

    /// <summary>Wraps the child's execution instance in this operator's own.</summary>
    protected abstract IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem> childReplacer)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem>(IReplacerInstance<TCandidate, TSearchSpace, TProblem> childReplacer)
    : ReplacerInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IReplacerInstance<TCandidate, TSearchSpace, TProblem> ChildReplacer { get; } = childReplacer;
}

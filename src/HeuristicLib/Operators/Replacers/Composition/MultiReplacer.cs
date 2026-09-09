using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

/// <remarks>
/// A multi replacer owns children, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged.
/// </remarks>
public abstract record MultiReplacer<TCandidate>
    : IReplacer<TCandidate>
{
    protected MultiReplacer(IReadOnlyList<IReplacer<TCandidate>> childReplacers)
    {
        ChildReplacers = childReplacers.ToValueArray();
    }

    public ValueArray<IReplacer<TCandidate>> ChildReplacers { get; init; }

    /// <summary>
    /// Resolves each child over the run's search space and problem and hands them to <see
    /// cref="CombineExecutionInstances{TRunSearchSpace, TRunProblem}"/>.
    /// </summary>
    public IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return CombineExecutionInstances([.. ChildReplacers.Select(child => resolver.Resolve(child))]);
    }

    /// <summary>Combines the children's execution instances into this operator's own.</summary>
    protected abstract IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem>> childReplacers)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class MultiReplacerInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> childReplacers)
    : ReplacerInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> ChildReplacers { get; } = childReplacers;
}

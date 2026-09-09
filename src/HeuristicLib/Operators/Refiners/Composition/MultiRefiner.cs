using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

/// <remarks>
/// A multi refiner owns children, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged.
/// </remarks>
public abstract record MultiRefiner<TCandidate>
    : IRefiner<TCandidate>
{
    protected MultiRefiner(IReadOnlyList<IRefiner<TCandidate>> childRefiners)
    {
        ChildRefiners = childRefiners.ToValueArray();
    }

    public ValueArray<IRefiner<TCandidate>> ChildRefiners { get; init; }

    /// <summary>
    /// Resolves each child over the run's search space and problem and hands them to <see
    /// cref="CombineExecutionInstances{TRunSearchSpace, TRunProblem}"/>.
    /// </summary>
    public IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return CombineExecutionInstances([.. ChildRefiners.Select(child => resolver.Resolve(child))]);
    }

    /// <summary>Combines the children's execution instances into this operator's own.</summary>
    protected abstract IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem>> childRefiners)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class MultiRefinerInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IRefinerInstance<TCandidate, TSearchSpace, TProblem>> childRefiners)
    : RefinerInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IRefinerInstance<TCandidate, TSearchSpace, TProblem>> ChildRefiners { get; } = childRefiners;
}

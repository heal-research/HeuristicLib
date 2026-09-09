using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

/// <remarks>
/// A wrapping refiner owns a child, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged.
/// </remarks>
public abstract record WrappingRefiner<TCandidate>
    : IRefiner<TCandidate>
{
    protected WrappingRefiner(IRefiner<TCandidate> childRefiner)
    {
        ChildRefiner = childRefiner;
    }

    public IRefiner<TCandidate> ChildRefiner { get; init; }

    /// <summary>
    /// Resolves the child over the run's search space and problem and hands it to <see
    /// cref="WrapExecutionInstance{TRunSearchSpace, TRunProblem}"/>.
    /// </summary>
    public IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace> =>
        WrapExecutionInstance(instanceRegistry.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildRefiner));

    /// <summary>Wraps the child's execution instance in this operator's own.</summary>
    protected abstract IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> childRefiner)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class WrappingRefinerInstance<TCandidate, TSearchSpace, TProblem>(IRefinerInstance<TCandidate, TSearchSpace, TProblem> childRefiner)
    : RefinerInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IRefinerInstance<TCandidate, TSearchSpace, TProblem> ChildRefiner { get; } = childRefiner;
}

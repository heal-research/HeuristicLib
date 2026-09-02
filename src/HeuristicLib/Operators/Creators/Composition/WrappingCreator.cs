using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

/// <remarks>
/// A wrapping creator owns a child, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged. Binding is a leaf concept: a composite that narrowed the triple would reject children the run
/// supports.
/// </remarks>
public abstract record WrappingCreator<TCandidate>
    : ICreator<TCandidate>
{
    protected WrappingCreator(ICreator<TCandidate> childCreator)
    {
        ChildCreator = childCreator;
    }

    public ICreator<TCandidate> ChildCreator { get; init; }

    /// <summary>
    /// Resolves the child over the run's search space and problem and hands it to
    /// <see cref="WrapExecutionInstance{TRunSearchSpace, TRunProblem}"/>. Left visible, because unlike a leaf
    /// creator this base offers no other creation member and hiding it would leave an author with no view of the
    /// mechanism their override plugs into.
    /// </summary>
    public ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace> =>
        WrapExecutionInstance(instanceRegistry.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildCreator));

    /// <summary>Wraps the child's execution instance in this operator's own.</summary>
    protected abstract ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childCreator)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem>(ICreatorInstance<TCandidate, TSearchSpace, TProblem> childCreator)
    : CreatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ICreatorInstance<TCandidate, TSearchSpace, TProblem> ChildCreator { get; } = childCreator;
}

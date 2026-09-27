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

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits([.. ChildRefiners]);


    /// <summary>
    /// Resolves each child over the run's search space and problem and hands them to <see
    /// cref="CombineExecutionInstances{TRunSearchSpace, TRunProblem}"/>.
    /// </summary>
    public IRefinerExecution<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return CombineExecutionInstances([.. ChildRefiners.Select(child => typed.Resolve(child))]);
    }

    /// <summary>Combines the children's execution nodes into this operator's own.</summary>
    protected abstract IRefinerExecution<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<IRefinerExecution<TCandidate, TRunSearchSpace, TRunProblem>> childRefiners)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class MultiRefinerExecution<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IRefinerExecution<TCandidate, TSearchSpace, TProblem>> childRefiners)
    : RefinerExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IRefinerExecution<TCandidate, TSearchSpace, TProblem>> ChildRefiners { get; } = childRefiners;
}

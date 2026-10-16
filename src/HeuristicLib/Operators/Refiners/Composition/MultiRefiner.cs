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

    /// <summary>Prepares this operator once, then binds its children in each construction scope.</summary>
    public ExecutorFactory<IRefinementExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var create = CreateCompositeFactory<TRunSearchSpace, TRunProblem>();
        return scope =>
        {
            var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem>();
            return create([.. ChildRefiners.Select(child => typed.Resolve(child))]);
        };
    }

    /// <summary>Prepares persistent execution data and returns a constructor accepting the resolved children.</summary>
    /// <remarks>Allocate shared state here; construct nodes with their contextual children in the returned delegate.</remarks>
    protected abstract CompositeExecutorFactory<IRefinementExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateCompositeFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class MultiRefinementExecutor<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IRefinementExecutor<TCandidate, TSearchSpace, TProblem>> childRefiners)
    : RefinementExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IRefinementExecutor<TCandidate, TSearchSpace, TProblem>> ChildRefiners { get; } = childRefiners;
}

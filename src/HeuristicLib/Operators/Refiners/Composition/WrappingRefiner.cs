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

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits(ChildRefiner);

    /// <summary>Prepares this operator once, then binds its child in each construction scope.</summary>
    public ExecutionFactory<IRefinerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var create = CreateWrapperFactory<TRunSearchSpace, TRunProblem>();
        return scope => create(scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildRefiner));
    }

    /// <summary>Prepares persistent execution data and returns a constructor accepting the resolved child.</summary>
    /// <remarks>Allocate shared state here; construct nodes with their contextual children in the returned delegate.</remarks>
    protected abstract WrapperExecutionFactory<IRefinerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class WrappingRefinerExecution<TCandidate, TSearchSpace, TProblem>(IRefinerExecution<TCandidate, TSearchSpace, TProblem> childRefiner)
    : RefinerExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IRefinerExecution<TCandidate, TSearchSpace, TProblem> ChildRefiner { get; } = childRefiner;
}

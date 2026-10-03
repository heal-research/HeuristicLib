using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

/// <remarks>
/// A wrapping interceptor owns a child, so it stays agnostic in the search space, problem and search state, and passes
/// the run's binding through to the child unchanged.
/// </remarks>
public abstract record WrappingInterceptor<TCandidate>
    : IInterceptor<TCandidate>
{
    protected WrappingInterceptor(IInterceptor<TCandidate> childInterceptor)
    {
        ChildInterceptor = childInterceptor;
    }

    public IInterceptor<TCandidate> ChildInterceptor { get; init; }

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits(ChildInterceptor);

    /// <summary>Prepares this operator once, then binds its child in each construction scope.</summary>
    public ExecutionFactory<IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateExecutionFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        var create = CreateWrapperFactory<TRunSearchSpace, TRunProblem, TRunSearchState>();
        return scope => create(scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>(ChildInterceptor));
    }

    /// <summary>Prepares persistent execution data and returns a constructor accepting the resolved child.</summary>
    /// <remarks>Allocate shared state here; construct nodes with their contextual children in the returned delegate.</remarks>
    protected abstract WrapperExecutionFactory<IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public abstract class WrappingInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor)
    : InterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState> ChildInterceptor { get; } = childInterceptor;
}

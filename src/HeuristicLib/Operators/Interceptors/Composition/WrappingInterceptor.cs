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


    /// <summary>
    /// Resolves the child over the run's search space and problem and hands it to <see
    /// cref="WrapExecutionInstance{TRunSearchSpace, TRunProblem, TRunSearchState}"/>.
    /// </summary>
    public IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ResolutionScope scope)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState =>
        WrapExecutionInstance(scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>(ChildInterceptor));

    /// <summary>Wraps the child's execution node in this operator's own.</summary>
    protected abstract IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> WrapExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> childInterceptor)
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

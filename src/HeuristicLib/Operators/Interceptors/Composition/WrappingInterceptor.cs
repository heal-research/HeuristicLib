using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

/// <remarks>
/// A wrapping interceptor owns a child, so it stays agnostic in the search space, problem and search state, and
/// passes the run's binding through to the child unchanged. Binding is a leaf concept.
/// </remarks>
public abstract record WrappingInterceptor<TCandidate>
    : IInterceptor<TCandidate>
{
    protected WrappingInterceptor(IInterceptor<TCandidate> childInterceptor)
    {
        ChildInterceptor = childInterceptor;
    }

    public IInterceptor<TCandidate> ChildInterceptor { get; init; }

    /// <summary>
    /// Resolves the child over the run's search space and problem and hands it to
    /// <see cref="WrapExecutionInstance{TRunSearchSpace, TRunProblem, TRunSearchState}"/>.
    /// </summary>
    public IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState =>
        WrapExecutionInstance(instanceRegistry.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>(ChildInterceptor));

    /// <summary>Wraps the child's execution instance in this operator's own.</summary>
    protected abstract IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> WrapExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> childInterceptor)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public abstract class WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor)
    : InterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> ChildInterceptor { get; } = childInterceptor;
}

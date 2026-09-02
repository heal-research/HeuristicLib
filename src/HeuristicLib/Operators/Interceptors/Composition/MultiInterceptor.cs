using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

/// <remarks>
/// A multi interceptor owns children, so it stays agnostic in the search space, problem and search state, and passes
/// the run's binding through to the children unchanged. See <see cref="WrappingInterceptor{TCandidate}"/> for why binding is a leaf concept.
/// </remarks>
public abstract record MultiInterceptor<TCandidate>
    : IInterceptor<TCandidate>
{
    protected MultiInterceptor(IReadOnlyList<IInterceptor<TCandidate>> childInterceptors)
    {
        ChildInterceptors = childInterceptors.ToValueArray();
    }

    public ValueArray<IInterceptor<TCandidate>> ChildInterceptors { get; init; }

    /// <summary>
    /// Resolves each child over the run's search space and problem and hands them to
    /// <see cref="CombineExecutionInstances{TRunSearchSpace, TRunProblem, TRunSearchState}"/>.
    /// </summary>
    public IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState =>
        CombineExecutionInstances([.. ChildInterceptors.Select(child => instanceRegistry.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>(child))]);

    /// <summary>Combines the children's execution instances into this operator's own.</summary>
    protected abstract IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CombineExecutionInstances<TRunSearchSpace, TRunProblem, TRunSearchState>(ImmutableArray<IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> childInterceptors)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public abstract class MultiInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(ImmutableArray<IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> childInterceptors)
    : InterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> ChildInterceptors { get; } = childInterceptors;
}

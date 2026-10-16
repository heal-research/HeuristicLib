using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

/// <remarks>
/// A multi interceptor owns children, so it stays agnostic in the search space, problem and search state, and passes
/// the run's binding through to the children unchanged.
/// </remarks>
public abstract record MultiInterceptor<TCandidate>
    : IInterceptor<TCandidate>
{
    protected MultiInterceptor(IReadOnlyList<IInterceptor<TCandidate>> childInterceptors)
    {
        ChildInterceptors = childInterceptors.ToValueArray();
    }

    public ValueArray<IInterceptor<TCandidate>> ChildInterceptors { get; init; }

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits([.. ChildInterceptors]);

    /// <summary>Prepares this operator once, then binds its children in each construction scope.</summary>
    public ExecutorFactory<IInterceptionExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateExecutorFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        var create = CreateCompositeFactory<TRunSearchSpace, TRunProblem, TRunSearchState>();
        return scope => create([.. ChildInterceptors.Select(child => scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>(child))]);
    }

    /// <summary>Prepares persistent execution data and returns a constructor accepting the resolved children.</summary>
    /// <remarks>Allocate shared state here; construct nodes with their contextual children in the returned delegate.</remarks>
    protected abstract CompositeExecutorFactory<IInterceptionExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateCompositeFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public abstract class MultiInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(ImmutableArray<IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>> childInterceptors)
    : InterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>> ChildInterceptors { get; } = childInterceptors;
}

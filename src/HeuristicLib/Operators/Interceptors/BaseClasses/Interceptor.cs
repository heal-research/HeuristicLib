using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

/// <remarks>
/// Derive directly from this base when the interceptor needs direct control over its execution structure.
/// Use <see cref="StatelessInterceptor{TCandidate,TSearchSpace,TProblem,TSearchState}"/> when no mutable execution data is needed.
/// <para>
/// The type arguments are the search space, problem and search state this interceptor is written for. The base bridges
/// to whatever a run requests, and a request the interceptor was not written for is reported when the execution graph
/// is built. An interceptor that owns children stays agnostic of the search space and problem and derives from
/// <see cref="WrappingOperator{TOperation, TCandidate}"/> or <see cref="MultiOperator{TOperation, TCandidate}"/> over
/// <see cref="Interception{TSearchState}"/> instead.
/// </para>
/// </remarks>
public abstract record Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Operator<Interception<TSearchState>, TCandidate, TSearchSpace, TProblem, TSearchState, IInterceptionExecutor<TSearchState>>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    /// <summary>An interceptor returns the search state, so that one must match exactly rather than convert.</summary>
    public override bool Supports(ExecutionSignature execution) => base.Supports(execution) && execution.SearchState == typeof(TSearchState);
}

public abstract record Interceptor<TCandidate, TSearchSpace, TSearchState>
    : Interceptor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Interceptor<TCandidate, TSearchState>
    : Interceptor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    where TSearchState : class, ISearchState;

public abstract class InterceptionExecutor<TSearchState>
    : IInterceptionExecutor<TSearchState>
    where TSearchState : class, ISearchState
{
    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random);
}

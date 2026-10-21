using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

/// <remarks>
/// Derive directly from this base when the interceptor needs direct control over its execution structure.
/// Use <see cref="StatelessInterceptor{TCandidate,TSearchSpace,TProblem,TSearchState}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulInterceptor{TCandidate,TSearchSpace,TProblem,TSearchState,TState}"/> when only ordinary execution data is needed.
/// <para>
/// The type arguments are the search space, problem and search state this interceptor is written for. The base bridges
/// to whatever a run requests, and a request the interceptor was not written for is reported when the execution graph
/// is built. An interceptor that owns children stays agnostic of the search space and problem and derives from
/// <see cref="WrappingOperator{TOperation, TCandidate}"/> or <see cref="MultiOperator{TOperation, TCandidate}"/> over
/// <see cref="Interception{TSearchState}"/> instead.
/// </para>
/// </remarks>
public abstract record Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Operator<Interception<TSearchState>, TCandidate, TSearchSpace, TProblem, TSearchState, IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    /// <summary>An interceptor returns the search state, so that one must match exactly rather than convert.</summary>
    public override bool Fits(ExecutionSignature execution) => base.Fits(execution) && execution.SearchState == typeof(TSearchState);
}

public abstract record Interceptor<TCandidate, TSearchSpace, TSearchState>
    : Interceptor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Interceptor<TCandidate, TSearchState>
    : Interceptor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    where TSearchState : class, ISearchState;

public abstract class InterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class InterceptionExecutor<TCandidate, TSearchSpace, TSearchState>
    : IInterceptionExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace);

    TSearchState IInterceptionExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>.Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Transform(currentState, previousState, random, searchSpace);
}

public abstract class InterceptionExecutor<TCandidate, TSearchState>
    : IInterceptionExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    where TSearchState : class, ISearchState
{
    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random);

    TSearchState IInterceptionExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>.Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Transform(currentState, previousState, random);
}

using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

/// <remarks>
/// Derive directly from this base when the interceptor owns child execution instances or needs direct control over its execution structure.
/// Use <see cref="StatelessInterceptor{TCandidate,TSearchSpace,TProblem,TSearchState}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulInterceptor{TCandidate,TSearchSpace,TProblem,TSearchState,TState}"/> when only ordinary execution data is needed.
/// </remarks>
public abstract record Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract record Interceptor<TCandidate, TSearchSpace, TSearchState>
    : Interceptor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Interceptor<TCandidate, TSearchState>
    : Interceptor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    where TSearchState : class, ISearchState;

public abstract class InterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class InterceptorInstance<TCandidate, TSearchSpace, TSearchState>
    : IInterceptorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace);

    TSearchState IInterceptorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>.Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Transform(currentState, previousState, random, searchSpace);
}

public abstract class InterceptorInstance<TCandidate, TSearchState>
    : IInterceptorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    where TSearchState : class, ISearchState
{
    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random);

    TSearchState IInterceptorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>.Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Transform(currentState, previousState, random);
}

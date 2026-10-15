using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record StatelessInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState>, IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public sealed override ExecutionFactory<IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>> CreateExecutionFactory() => _ => this;

    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessInterceptor<TCandidate, TSearchSpace, TSearchState>
    : Interceptor<TCandidate, TSearchSpace, TSearchState>, IInterceptorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public sealed override ExecutionFactory<IInterceptorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>> CreateExecutionFactory() => _ => this;

    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace);

    TSearchState IInterceptorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>.Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Transform(currentState, previousState, random, searchSpace);
}

public abstract record StatelessInterceptor<TCandidate, TSearchState>
    : Interceptor<TCandidate, TSearchState>, IInterceptorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    where TSearchState : class, ISearchState
{
    public sealed override ExecutionFactory<IInterceptorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>> CreateExecutionFactory() => _ => this;

    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random);

    TSearchState IInterceptorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>.Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Transform(currentState, previousState, random);
}

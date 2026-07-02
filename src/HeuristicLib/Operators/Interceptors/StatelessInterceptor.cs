using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record StatelessInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  : IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>,
    IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessInterceptor<TCandidate, TSearchSpace, TSearchState>
  : IInterceptor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>,
    IInterceptorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public IInterceptorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace);

    TSearchState IInterceptorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>.Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
      Transform(currentState, previousState, searchSpace);
}

public abstract record StatelessInterceptor<TCandidate, TSearchState>
  : IInterceptor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>,
    IInterceptorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
  where TSearchState : class, ISearchState
{
    public IInterceptorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState);

    TSearchState IInterceptorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>.Transform(TSearchState currentState, TSearchState? previousState, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
      Transform(currentState, previousState);
}

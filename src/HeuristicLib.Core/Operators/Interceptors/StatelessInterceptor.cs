using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record StatelessInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>
  : IInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>,
    IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessInterceptor<TGenotype, TSearchSpace, TSearchState>
  : IInterceptor<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TSearchState>,
    IInterceptorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public IInterceptorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace);

  TSearchState IInterceptorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TSearchState>.Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) =>
    Transform(currentState, previousState, searchSpace);
}

public abstract record StatelessInterceptor<TGenotype, TSearchState>
  : IInterceptor<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TSearchState>,
    IInterceptorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TSearchState>
  where TSearchState : class, ISearchState
{
  public IInterceptorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState);

  TSearchState IInterceptorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TSearchState>.Transform(TSearchState currentState, TSearchState? previousState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
    Transform(currentState, previousState);
}

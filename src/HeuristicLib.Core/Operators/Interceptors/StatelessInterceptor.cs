using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record StatelessInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>,
    IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessInterceptor<TGenotype, TSearchSpace, TAlgorithmState>
  : IInterceptor<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState>,
    IInterceptorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public IInterceptorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace);

  TAlgorithmState IInterceptorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState>.Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) =>
    Transform(currentState, previousState, searchSpace);
}

public abstract record StatelessInterceptor<TGenotype, TAlgorithmState>
  : IInterceptor<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState>,
    IInterceptorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
{
  public IInterceptorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

  public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState);

  TAlgorithmState IInterceptorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState>.Transform(TAlgorithmState currentState, TAlgorithmState? previousState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
    Transform(currentState, previousState);
}

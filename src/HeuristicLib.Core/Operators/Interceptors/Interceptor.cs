using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record Interceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);

  public abstract class Instance
    : IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem);
  }
}

public abstract record Interceptor<TGenotype, TSearchSpace, TAlgorithmState>
  : IInterceptor<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract IInterceptorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);

  public abstract class Instance
    : IInterceptorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState>
  {
    public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace);

    TAlgorithmState IInterceptorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState>.Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) =>
      Transform(currentState, previousState, searchSpace);
  }
}

public abstract record Interceptor<TGenotype, TAlgorithmState>
  : IInterceptor<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
{
  public abstract IInterceptorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);

  public abstract class Instance
    : IInterceptorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState>
  {
    public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState);

    TAlgorithmState IInterceptorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState>.Transform(TAlgorithmState currentState, TAlgorithmState? previousState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
      Transform(currentState, previousState);
  }
}

using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record Interceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  : IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);

  public abstract class Instance
    : IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  {
    public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem);
  }
}

public abstract record Interceptor<TGenotype, TAlgorithmState, TSearchSpace>
  : IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);

  public abstract class Instance
    : IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace);

    TAlgorithmState IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) =>
      Transform(currentState, previousState, searchSpace);
  }
}

public abstract record Interceptor<TGenotype, TAlgorithmState>
  : IInterceptor<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TAlgorithmState : class, IAlgorithmState
{
  public abstract IInterceptorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);

  public abstract class Instance
    : IInterceptorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState);

    TAlgorithmState IInterceptorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Transform(TAlgorithmState currentState, TAlgorithmState? previousState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
      Transform(currentState, previousState);
  }
}

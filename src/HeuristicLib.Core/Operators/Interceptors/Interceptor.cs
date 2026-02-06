using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract class Interceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem> 
  : IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  where TGenotype : class
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class InterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem> 
  : IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  where TGenotype : class
 where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem);
}


public abstract class Interceptor<TGenotype, TAlgorithmState, TSearchSpace> 
  : IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TGenotype : class
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class InterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace> 
  : IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TGenotype : class
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace);

  TAlgorithmState IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => 
    Transform(currentState, previousState, searchSpace);
}


public abstract class Interceptor<TGenotype, TAlgorithmState> 
  : IInterceptor<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TGenotype : class
  where TAlgorithmState : class, IAlgorithmState
{
  public abstract IInterceptorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class InterceptorInstance<TGenotype, TAlgorithmState>
  : IInterceptorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TGenotype : class
  where TAlgorithmState : class, IAlgorithmState
{
  public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState);

  TAlgorithmState IInterceptorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Transform(TAlgorithmState currentState, TAlgorithmState? previousState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => 
    Transform(currentState, previousState);
}


public abstract class StatelessInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem> 
  : IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>,
    IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  where TGenotype : class
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
  
  public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem);
}

public abstract class StatelessInterceptor<TGenotype, TAlgorithmState, TSearchSpace> 
  : IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>,
    IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TGenotype : class
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
  
  public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace);

  TAlgorithmState IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => 
    Transform(currentState, previousState, searchSpace);
}

public abstract class StatelessInterceptor<TGenotype, TAlgorithmState> 
  : IInterceptor<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>,
    IInterceptorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TGenotype : class
  where TAlgorithmState : class, IAlgorithmState
{
  public IInterceptorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
  
  public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState);

  TAlgorithmState IInterceptorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Transform(TAlgorithmState currentState, TAlgorithmState? previousState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => 
    Transform(currentState, previousState);
}

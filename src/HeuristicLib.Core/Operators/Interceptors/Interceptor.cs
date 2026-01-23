using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract class Interceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem> : IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  where TAlgorithmState : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem);
}

public abstract class Interceptor<TGenotype, TAlgorithmState, TSearchSpace> : IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TAlgorithmState : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace);

  TAlgorithmState IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => Transform(currentState, previousState, searchSpace);
}

public abstract class Interceptor<TGenotype, TAlgorithmState> : IInterceptor<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TAlgorithmState : IAlgorithmState
{
  public abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState);

  TAlgorithmState IInterceptor<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Transform(TAlgorithmState currentState, TAlgorithmState? previousState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Transform(currentState, previousState);
}

public abstract class Interceptor<TGenotype> : IInterceptor<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract IAlgorithmState Transform(IAlgorithmState currentState, IAlgorithmState? previousState);

  IAlgorithmState IInterceptor<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Transform(IAlgorithmState currentState, IAlgorithmState? previousState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Transform(currentState, previousState);
}

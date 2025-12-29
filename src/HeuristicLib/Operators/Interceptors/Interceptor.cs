using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract class Interceptor<TGenotype, TIterationState, TSearchSpace, TProblem> : IInterceptor<TGenotype, TIterationState, TSearchSpace, TProblem>
  where TIterationState : IIterationState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public abstract TIterationState Transform(TIterationState currenTIterationState, TIterationState? previousIterationState, TSearchSpace searchSpace, TProblem problem);
}

public abstract class Interceptor<TGenotype, TIterationState, TSearchSpace> : IInterceptor<TGenotype, TIterationState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TIterationState : IIterationState
  where TSearchSpace : class, ISearchSpace<TGenotype> {
  public abstract TIterationState Transform(TIterationState currenTIterationState, TIterationState? previousIterationState, TSearchSpace searchSpace);

  TIterationState IInterceptor<TGenotype, TIterationState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Transform(TIterationState currenTIterationState, TIterationState? previousIterationState, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) {
    return Transform(currenTIterationState, previousIterationState, searchSpace);
  }
}

public abstract class Interceptor<TGenotype, TIterationState> : IInterceptor<TGenotype, TIterationState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TIterationState : IIterationState {
  public abstract TIterationState Transform(TIterationState currenTIterationState, TIterationState? previousIterationState);

  TIterationState IInterceptor<TGenotype, TIterationState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Transform(TIterationState currenTIterationState, TIterationState? previousIterationState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
    return Transform(currenTIterationState, previousIterationState);
  }
}

public abstract class Interceptor<TGenotype> : IInterceptor<TGenotype, IIterationState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> {
  public abstract IIterationState Transform(IIterationState currentIterationState, IIterationState? previousIterationState);

  IIterationState IInterceptor<TGenotype, IIterationState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Transform(IIterationState currentIterationState, IIterationState? previousIterationState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
    return Transform(currentIterationState, previousIterationState);
  }
}

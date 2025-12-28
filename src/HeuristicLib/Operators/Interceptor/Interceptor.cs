using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptor;

public abstract class Interceptor<TGenotype, TIterationResult, TSearchSpace, TProblem> : IInterceptor<TGenotype, TIterationResult, TSearchSpace, TProblem>
  where TIterationResult : IIterationResult
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public abstract TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TSearchSpace searchSpace, TProblem problem);
}

public abstract class Interceptor<TGenotype, TIterationResult, TSearchSpace> : IInterceptor<TGenotype, TIterationResult, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TIterationResult : IIterationResult
  where TSearchSpace : class, ISearchSpace<TGenotype> {
  public abstract TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TSearchSpace searchSpace);

  TIterationResult IInterceptor<TGenotype, TIterationResult, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) {
    return Transform(currentIterationResult, previousIterationResult, searchSpace);
  }
}

public abstract class Interceptor<TGenotype, TIterationResult> : IInterceptor<TGenotype, TIterationResult, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TIterationResult : IIterationResult {
  public abstract TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult);

  TIterationResult IInterceptor<TGenotype, TIterationResult, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
    return Transform(currentIterationResult, previousIterationResult);
  }
}

public abstract class Interceptor<TGenotype> : IInterceptor<TGenotype, IIterationResult, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> {
  public abstract IIterationResult Transform(IIterationResult currentIterationResult, IIterationResult? previousIterationResult);

  IIterationResult IInterceptor<TGenotype, IIterationResult, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Transform(IIterationResult currentIterationResult, IIterationResult? previousIterationResult, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
    return Transform(currentIterationResult, previousIterationResult);
  }
}

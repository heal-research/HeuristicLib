using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract class Interceptor<TGenotype, TIterationResult, TSearchSpace, TProblem> : IInterceptor<TGenotype, TIterationResult, TSearchSpace, TProblem>
  where TIterationResult : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem);
}

public abstract class Interceptor<TGenotype, TIterationResult, TSearchSpace> : IInterceptor<TGenotype, TIterationResult, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TIterationResult : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{

  TIterationResult IInterceptor<TGenotype, TIterationResult, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IRandomNumberGenerator random, TSearchSpace encoding, IProblem<TGenotype, TSearchSpace> problem) => Transform(currentIterationResult, previousIterationResult, random, encoding);
  public abstract TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IRandomNumberGenerator random, TSearchSpace encoding);
}

public abstract class Interceptor<TGenotype, TIterationResult> : IInterceptor<TGenotype, TIterationResult, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TIterationResult : IAlgorithmState
{

  TIterationResult IInterceptor<TGenotype, TIterationResult, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Transform(currentIterationResult, previousIterationResult, random);
  public abstract TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult, IRandomNumberGenerator random);
}

public abstract class Interceptor<TGenotype> : IInterceptor<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{

  IAlgorithmState IInterceptor<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Transform(IAlgorithmState currentAlgorithmState, IAlgorithmState? previousIterationResult, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Transform(currentAlgorithmState, previousIterationResult, random);
  public abstract IAlgorithmState Transform(IAlgorithmState currentAlgorithmState, IAlgorithmState? previousIterationResult, IRandomNumberGenerator random);
}

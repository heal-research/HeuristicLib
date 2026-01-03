using HEAL.HeuristicLib.Observation;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm<TGenotype, in TSearchSpace, in TProblem, TAlgorithmState> 
  :  IExecutable<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  , IIterable<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  , IObservable<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{
  // This is an aggregate interface
}

public interface IObservable<in TG, in TS, in TP, in TR> 
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  IIterationObserver<TG, TS, TP, TR>? Observer { get; }
}

public interface IIterable<TG, in TS, in TP, TR>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  // [Obsolete("Think about removing this from core interface")]
  
  ITerminator<TG, TR, TS, TP> Terminator { get; }
  
  IInterceptor<TG, TR, TS, TP>? Interceptor { get; }
  
  TR ExecuteStep(TP problem, TR? previousState, IRandomNumberGenerator random);
}

public static class IIterableExtensions {
  extension<TG, TS, TP, TR> (IIterable<TG, TS, TP, TR> iterable)
    where TG : class
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : class, IAlgorithmState
  {
    public TR Execute(TP problem, IRandomNumberGenerator random, TR? initialState = null) {
      return iterable.ExecuteStreaming(problem, random, initialState).Last();
    }
    
    public IEnumerable<TR> ExecuteStreaming(TP problem, IRandomNumberGenerator random, TR? initialState = null) {
      TR? previousState = initialState;
      bool shouldContinue = previousState is null || iterable.Terminator.ShouldContinue(previousState, previousIterationState: default, problem.SearchSpace, problem);

      while (shouldContinue) {
        var newIterationState = iterable.ExecuteStep(problem, previousState, random);
        if (iterable.Interceptor is not null) {
          newIterationState = iterable.Interceptor.Transform(newIterationState, previousState, problem.SearchSpace, problem);
        }
      
        yield return newIterationState;
      
        shouldContinue = iterable.Terminator.ShouldContinue(newIterationState, previousState, problem.SearchSpace, problem);
        previousState = newIterationState;
      }
    }
  }
}

public interface IExecutable<TG, in TS, in TP, out TR>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : IAlgorithmState
{
  TR Execute(TP problem, IRandomNumberGenerator random);
}

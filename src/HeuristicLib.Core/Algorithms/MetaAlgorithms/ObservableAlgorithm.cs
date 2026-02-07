using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public class ObservableAlgorithm<TG, TS, TP, TR>
  : IAlgorithm<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  private readonly IAlgorithm<TG, TS, TP, TR> algorithm;
  private readonly IReadOnlyList<IAlgorithmObserver<TG, TS, TP, TR>> observers;

  public ObservableAlgorithm(IAlgorithm<TG, TS, TP, TR> algorithm, params IReadOnlyList<IAlgorithmObserver<TG, TS, TP, TR>> observers)
  {
    this.algorithm = algorithm;
    this.observers = observers;
  }
  
  public IAlgorithmInstance<TG, TS, TP, TR> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    return new ObservableAlgorithmInstance<TG, TS, TP, TR>(
      algorithm.CreateExecutionInstance(instanceRegistry), // ToDo: think if this is correct
      observers
    );
  }
}

public class ObservableAlgorithmInstance<TG, TS, TP, TR> 
  : IAlgorithmInstance<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  private readonly IAlgorithmInstance<TG, TS, TP, TR> algorithmInstance;
  private readonly IReadOnlyList<IAlgorithmObserver<TG, TS, TP, TR>> observers;
  
  public ObservableAlgorithmInstance(IAlgorithmInstance<TG, TS, TP, TR> algorithmInstance, IReadOnlyList<IAlgorithmObserver<TG, TS, TP, TR>> observers)
  {
    this.algorithmInstance = algorithmInstance;
    this.observers = observers;
  }
  
  public IAsyncEnumerable<TR> RunStreamingAsync(
    TP problem,
    IRandomNumberGenerator random,
    TR? initialState = null,
    CancellationToken ct = default
  )
  {
    TR? previousState = initialState;

    return algorithmInstance
      .RunStreamingAsync(problem, random, initialState, ct)
      .Select(currentState => {
        foreach (var observer in observers) {
          observer.AfterIteration(currentState, previousState, random, problem);
        }

        previousState = currentState;
        return currentState;
      });
  }
}

public interface IAlgorithmObserver<TG, in TS, in TP, in TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  void AfterIteration(TR currentState, TR? previousState, IRandomNumberGenerator random, TP problem);
}

public class AlgorithmObserver<TG, TS, TP, TR> : IAlgorithmObserver<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  private readonly Action<TR, TR?, IRandomNumberGenerator, TP> afterIteration;
  
  public AlgorithmObserver(Action<TR, TR?, IRandomNumberGenerator, TP> afterIteration)
  {
    this.afterIteration = afterIteration;
  }
  
  public void AfterIteration(TR currentState, TR? previousState, IRandomNumberGenerator random, TP problem)
  {
    afterIteration(currentState, previousState, random, problem);
  }
}


public static class ObservableAlgorithmExtensions
{
  extension<TG, TS, TP, TR>(IAlgorithm<TG, TS, TP, TR> algorithm)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : class, IAlgorithmState
  {
    public IAlgorithm<TG, TS, TP, TR> ObserveWith(IAlgorithmObserver<TG, TS, TP, TR> observer)
    {
      return new ObservableAlgorithm<TG, TS, TP, TR>(algorithm, observer);
    }
    public IAlgorithm<TG, TS, TP, TR>  ObserveWith(params IReadOnlyList<IAlgorithmObserver<TG, TS, TP, TR>> observers)
    {
      return new ObservableAlgorithm<TG, TS, TP, TR>(algorithm, observers);
    }
    
    public IAlgorithm<TG, TS, TP, TR> ObserveWith(Action<TR, TR?, IRandomNumberGenerator, TP> afterIteration)
    {
      var observer = new AlgorithmObserver<TG, TS, TP, TR>(afterIteration);
      return algorithm.ObserveWith(observer);
    }
    
    public IAlgorithm<TG, TS, TP, TR> ObserveWith(Action<TR> afterIteration)
    {
      var observer = new AlgorithmObserver<TG, TS, TP, TR>((state,  _, _, _) => afterIteration(state));
      return algorithm.ObserveWith(observer);
    }
  }
}

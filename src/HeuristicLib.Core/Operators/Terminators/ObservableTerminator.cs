using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class ObservableTerminator<TG, TR, TS, TP>
  : Terminator<TG, TR, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  private readonly ITerminator<TG, TR, TS, TP> interceptor;
  private readonly IReadOnlyList<ITerminatorObserver<TG, TR, TS, TP>> observers;
  
  public ObservableTerminator(ITerminator<TG, TR, TS, TP> interceptor, params IReadOnlyList<ITerminatorObserver<TG, TR, TS, TP>> observers)
  {
    this.interceptor = interceptor;
    this.observers = observers;
  }
  
  public override ITerminatorInstance<TG, TR, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var interceptorInstance = instanceRegistry.GetOrCreate(interceptor);
    return new ObservableTerminatorInstance(interceptorInstance, observers);
  }

  private sealed class ObservableTerminatorInstance(ITerminatorInstance<TG, TR, TS, TP> terminatorInstance, IReadOnlyList<ITerminatorObserver<TG, TR, TS, TP>> observers) 
    : TerminatorInstance<TG, TR, TS, TP>
  {
    public override bool ShouldTerminate(TR state, TS searchSpace, TP problem)
    {
      var result = terminatorInstance.ShouldTerminate(state, searchSpace, problem);

      foreach (var observer in observers) {
        observer.AfterTerminationCheck(result, state, searchSpace, problem);
      }
      
      return result;
    }
  }
}

public interface ITerminatorObserver<in TG, in TR, in TS, in TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  void AfterTerminationCheck(bool shouldTerminate, TR state, TS searchSpace, TP problem);
}

// ToDo: rename to make it clear that this is not a base-class to be inherited from
public class TerminatorObserver<TG, TR, TS, TP> : ITerminatorObserver<TG, TR, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  private readonly Action<bool, TR, TS, TP> afterTerminateCheck;
  
  public TerminatorObserver(Action<bool, TR, TS, TP> afterTerminateCheck)
  {
    this.afterTerminateCheck = afterTerminateCheck;
  }

  public void AfterTerminationCheck(bool shouldTerminate, TR state, TS searchSpace, TP problem) 
  {
    afterTerminateCheck.Invoke(shouldTerminate, state, searchSpace, problem);
  }
}

public static class ObservableTerminatorExtensions
{
  extension<TG, TR, TS, TP>(ITerminator<TG, TR, TS, TP> terminator)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : class, IAlgorithmState
  {
    public ITerminator<TG, TR, TS, TP> ObserveWith(ITerminatorObserver<TG, TR, TS, TP> observer)
    {
      return new ObservableTerminator<TG, TR, TS, TP>(terminator, observer);
    }
    
    public ITerminator<TG, TR, TS, TP> ObserveWith(params IReadOnlyList<ITerminatorObserver<TG, TR, TS, TP>> observers)
    {
      return new ObservableTerminator<TG, TR, TS, TP>(terminator, observers);
    }
    
    public ITerminator<TG, TR, TS, TP> ObserveWith(Action<bool, TR, TS, TP> afterTerminationCheck)
    {
      var observer = new TerminatorObserver<TG, TR, TS, TP>(afterTerminationCheck);
      return terminator.ObserveWith(observer);
    }
    
    public ITerminator<TG, TR, TS, TP> ObserveWith(Action<bool> afterTerminationCheck)
    {
      var observer = new TerminatorObserver<TG, TR, TS, TP>((shouldTerminate, _, _, _) => afterTerminationCheck(shouldTerminate));
      return terminator.ObserveWith(observer);
    }
    
    public ITerminator<TG, TR, TS, TP> CountInvocations(InvocationCounter counter)
    {
      return terminator.ObserveWith(_ => counter.IncrementBy(1));
    }
    
    public ITerminator<TG, TR, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return terminator.CountInvocations(counter);
    }
  }
}

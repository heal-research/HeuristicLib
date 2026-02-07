using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public class ObservableInterceptor<TG, TR, TS, TP>
  : Interceptor<TG, TR, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  private readonly IInterceptor<TG, TR, TS, TP> interceptor;
  private readonly IReadOnlyList<IInterceptorObserver<TG, TR, TS, TP>> observers;
  
  public ObservableInterceptor(IInterceptor<TG, TR, TS, TP> interceptor, params IReadOnlyList<IInterceptorObserver<TG, TR, TS, TP>> observers)
  {
    this.interceptor = interceptor;
    this.observers = observers;
  }
  
  public override IInterceptorInstance<TG, TR, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var interceptorInstance = instanceRegistry.GetOrCreate(interceptor);
    return new ObservableInterceptorInstance(interceptorInstance, observers);
  }

  private sealed class ObservableInterceptorInstance(IInterceptorInstance<TG, TR, TS, TP> interceptorInstance, IReadOnlyList<IInterceptorObserver<TG, TR, TS, TP>> observers) 
    : InterceptorInstance<TG, TR, TS, TP>
  {
    public override TR Transform(TR currentState, TR? previousState, TS searchSpace, TP problem)
    {
      var result = interceptorInstance.Transform(currentState, previousState, searchSpace, problem);

      foreach (var observer in observers) {
        observer.AfterInterception(result, currentState, previousState, searchSpace, problem);
      }
      
      return result;
    }
  }
}

public interface IInterceptorObserver<in TG, in TR, in TS, in TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  // ToDo: probably remove the random for observation
  void AfterInterception(TR newState, TR currentState, TR? previousState, TS searchSpace, TP problem);
}

// ToDo: rename to make it clear that this is not a base-class to be inherited from
public class InterceptorObserver<TG, TR, TS, TP> : IInterceptorObserver<TG, TR, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  private readonly Action<TR, TR, TR?, TS, TP> afterInterception;
  
  public InterceptorObserver(Action<TR, TR, TR?, TS, TP> afterInterception)
  {
    this.afterInterception = afterInterception;
  }

  public void AfterInterception(TR newState, TR currentState, TR? previousState, TS searchSpace, TP problem) 
  {
    afterInterception.Invoke(newState, currentState, previousState, searchSpace, problem);
  }
}

public static class ObservableInterceptorExtensions
{
  extension<TG, TR, TS, TP>(IInterceptor<TG, TR, TS, TP> interceptor)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : class, IAlgorithmState
  {
    public IInterceptor<TG, TR, TS, TP> ObserveWith(IInterceptorObserver<TG, TR, TS, TP> observer)
    {
      return new ObservableInterceptor<TG, TR, TS, TP>(interceptor, observer);
    }
    
    public IInterceptor<TG, TR, TS, TP> ObserveWith(params IReadOnlyList<IInterceptorObserver<TG, TR, TS, TP>> observers)
    {
      return new ObservableInterceptor<TG, TR, TS, TP>(interceptor, observers);
    }
    
    public IInterceptor<TG, TR, TS, TP> ObserveWith(Action<TR, TR, TR?, TS, TP> afterInterception)
    {
      var observer = new InterceptorObserver<TG, TR, TS, TP>(afterInterception);
      return interceptor.ObserveWith(observer);
    }
    
    public IInterceptor<TG, TR, TS, TP> ObserveWith(Action<TR> afterInterception)
    {
      var observer = new InterceptorObserver<TG, TR, TS, TP>((newState, _, _, _, _) => afterInterception(newState));
      return interceptor.ObserveWith(observer);
    }
    
    public IInterceptor<TG, TR, TS, TP> CountInvocations(InvocationCounter counter)
    {
      return interceptor.ObserveWith(_ => counter.IncrementBy(1));
    }
    
    public IInterceptor<TG, TR, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return interceptor.CountInvocations(counter);
    }
  }
}

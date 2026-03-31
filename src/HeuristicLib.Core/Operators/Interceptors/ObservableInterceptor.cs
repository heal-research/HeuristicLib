using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

[Equatable]
public partial record ObservableInterceptor<TG, TS, TP, TR>
  : IInterceptor<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  public IInterceptor<TG, TS, TP, TR> Interceptor { get; }

  [OrderedEquality]
  public ImmutableArray<IInterceptorObserver<TG, TS, TP, TR>> Observers { get; }

  public ObservableInterceptor(IInterceptor<TG, TS, TP, TR> interceptor, params ImmutableArray<IInterceptorObserver<TG, TS, TP, TR>> observers)
  {
    Interceptor = interceptor;
    Observers = observers;
  }

  public IInterceptorInstance<TG, TS, TP, TR> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var interceptorInstance = instanceRegistry.Resolve(Interceptor);
    var interceptorObserverInstances = Observers.Select(instanceRegistry.Resolve).ToArray();
    return new Instance(interceptorInstance, interceptorObserverInstances);
  }

  private sealed class Instance(IInterceptorInstance<TG, TS, TP, TR> interceptorInstance, IReadOnlyList<IInterceptorObserverInstance<TG, TS, TP, TR>> observers)
    : IInterceptorInstance<TG, TS, TP, TR>
  {
    public TR Transform(TR currentState, TR? previousState, TS searchSpace, TP problem)
    {
      var result = interceptorInstance.Transform(currentState, previousState, searchSpace, problem);

      foreach (var observer in observers) {
        observer.AfterInterception(result, currentState, previousState, searchSpace, problem);
      }

      return result;
    }
  }
}

public interface IInterceptorObserver<in TG, in TS, in TP, in TR> : IExecutable<IInterceptorObserverInstance<TG, TS, TP, TR>>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState;

public interface IInterceptorObserverInstance<in TG, in TS, in TP, in TR> : IExecutionInstance
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  void AfterInterception(TR newState, TR currentState, TR? previousState, TS searchSpace, TP problem);
}

public class ActionInterceptorObserver<TG, TR, TS, TP> : IInterceptorObserver<TG, TS, TP, TR>, IInterceptorObserverInstance<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  private readonly Action<TR, TR, TR?, TS, TP> afterInterception;

  public ActionInterceptorObserver(Action<TR, TR, TR?, TS, TP> afterInterception)
  {
    this.afterInterception = afterInterception;
  }

  public void AfterInterception(TR newState, TR currentState, TR? previousState, TS searchSpace, TP problem)
  {
    afterInterception.Invoke(newState, currentState, previousState, searchSpace, problem);
  }

  public IInterceptorObserverInstance<TG, TS, TP, TR> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
}

public static class ObservableInterceptorExtensions
{
  extension<TG, TS, TP, TR>(IInterceptor<TG, TS, TP, TR> interceptor)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : class, IAlgorithmState
  {
    public IInterceptor<TG, TS, TP, TR> ObserveWith(IInterceptorObserver<TG, TS, TP, TR> observer)
    {
      return new ObservableInterceptor<TG, TS, TP, TR>(interceptor, observer);
    }

    public IInterceptor<TG, TS, TP, TR> ObserveWith(params ImmutableArray<IInterceptorObserver<TG, TS, TP, TR>> observers)
    {
      return new ObservableInterceptor<TG, TS, TP, TR>(interceptor, observers);
    }

    public IInterceptor<TG, TS, TP, TR> ObserveWith(Action<TR, TR, TR?, TS, TP> afterInterception)
    {
      var observer = new ActionInterceptorObserver<TG, TR, TS, TP>(afterInterception);
      return interceptor.ObserveWith(observer);
    }

    public IInterceptor<TG, TS, TP, TR> ObserveWith(Action<TR> afterInterception)
    {
      var observer = new ActionInterceptorObserver<TG, TR, TS, TP>((newState, _, _, _, _) => afterInterception(newState));
      return interceptor.ObserveWith(observer);
    }

    public IInterceptor<TG, TS, TP, TR> CountInvocations(InvocationCounter counter)
    {
      return interceptor.ObserveWith(_ => counter.IncrementBy(1));
    }

    public IInterceptor<TG, TS, TP, TR> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return interceptor.CountInvocations(counter);
    }
  }
}

using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

[Equatable]
public partial record ObservableInterceptor<TG, TS, TP, TR>
  : WrappingInterceptor<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, ISearchState
{
  [OrderedEquality]
  public ImmutableArray<IInterceptorObserver<TG, TS, TP, TR>> Observers { get; }

  public ObservableInterceptor(IInterceptor<TG, TS, TP, TR> interceptor, ImmutableArray<IInterceptorObserver<TG, TS, TP, TR>> observers)
    : base(interceptor)
  {
    Observers = observers;
  }

  public ObservableInterceptor(IInterceptor<TG, TS, TP, TR> interceptor, params IEnumerable<IInterceptorObserver<TG, TS, TP, TR>> observers)
    : this(interceptor, [.. observers])
  {
  }

  protected override TR Transform(TR currentState, TR? previousState, InnerTransform innerTransform, TS searchSpace, TP problem)
  {
    var result = innerTransform(currentState, previousState, searchSpace, problem);
    foreach (var observer in Observers) {
      observer.AfterInterception(result, currentState, previousState, searchSpace, problem);
    }
    return result;
  }
}

public interface IInterceptorObserver<in TG, in TS, in TP, in TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, ISearchState
{
  void AfterInterception(TR newState, TR currentState, TR? previousState, TS searchSpace, TP problem);
}

public static class ObservableInterceptorExtensions
{
  extension<TG, TS, TP, TR>(IInterceptor<TG, TS, TP, TR> interceptor)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : class, ISearchState
  {
    public IInterceptor<TG, TS, TP, TR> ObserveWith(IInterceptorObserver<TG, TS, TP, TR> observer)
      => new ObservableInterceptor<TG, TS, TP, TR>(interceptor, observer);
    public IInterceptor<TG, TS, TP, TR> ObserveWith(params IEnumerable<IInterceptorObserver<TG, TS, TP, TR>> observers)
      => new ObservableInterceptor<TG, TS, TP, TR>(interceptor, observers);
    public IInterceptor<TG, TS, TP, TR> ObserveWith(Action<TR, TR, TR?, TS, TP> afterInterception)
      => interceptor.ObserveWith(new ActionInterceptorObserver<TG, TS, TP, TR>(afterInterception));
    public IInterceptor<TG, TS, TP, TR> ObserveWith(Action<TR> afterInterception)
      => interceptor.ObserveWith(new ActionInterceptorObserver<TG, TS, TP, TR>((newState, _, _, _, _) => afterInterception(newState)));
    public IInterceptor<TG, TS, TP, TR> CountInvocations(InvocationCounter counter)
      => interceptor.ObserveWith(_ => counter.IncrementBy(1));
    public IInterceptor<TG, TS, TP, TR> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return interceptor.CountInvocations(counter);
    }
  }
}

public sealed class ActionInterceptorObserver<TG, TS, TP, TR>(Action<TR, TR, TR?, TS, TP> afterInterception) : IInterceptorObserver<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, ISearchState
{
  public void AfterInterception(TR newState, TR currentState, TR? previousState, TS searchSpace, TP problem) => afterInterception(newState, currentState, previousState, searchSpace, problem);
}

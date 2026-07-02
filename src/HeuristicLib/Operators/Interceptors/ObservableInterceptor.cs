using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

[Equatable]
public partial record ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  : WrappingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    [OrderedEquality]
    public ImmutableArray<IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> Observers { get; }

    public ObservableInterceptor(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor, ImmutableArray<IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
      : base(interceptor)
    {
        Observers = observers;
    }

    public ObservableInterceptor(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor, params IEnumerable<IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
      : this(interceptor, [.. observers])
    {
    }

    protected override TSearchState Transform(TSearchState currentState, TSearchState? previousState, InnerTransform innerTransform, TSearchSpace searchSpace, TProblem problem)
    {
        var result = innerTransform(currentState, previousState, searchSpace, problem);
        foreach (var observer in Observers)
        {
            observer.AfterInterception(result, currentState, previousState, searchSpace, problem);
        }
        return result;
    }
}

public interface IInterceptorObserver<in TCandidate, in TSearchSpace, in TProblem, in TSearchState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    void AfterInterception(TSearchState newState, TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem);
}

public static class ObservableInterceptorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor)
      where TSearchSpace : class, ISearchSpace<TCandidate>
      where TProblem : class, IProblem<TCandidate, TSearchSpace>
      where TSearchState : class, ISearchState
    {
        public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState> observer)
          => new ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, observer);
        public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(params IEnumerable<IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
          => new ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, observers);
        public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(Action<TSearchState, TSearchState, TSearchState?, TSearchSpace, TProblem> afterInterception)
          => interceptor.ObserveWith(new ActionInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(afterInterception));
        public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(Action<TSearchState> afterInterception)
          => interceptor.ObserveWith(new ActionInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>((newState, _, _, _, _) => afterInterception(newState)));
    }
}

public sealed class ActionInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(Action<TSearchState, TSearchState, TSearchState?, TSearchSpace, TProblem> afterInterception) : IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    public void AfterInterception(TSearchState newState, TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem) => afterInterception(newState, currentState, previousState, searchSpace, problem);
}

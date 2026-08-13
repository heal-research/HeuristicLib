using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public record ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  : WrappingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Interceptor => InnerInterceptor;

    public ValueArray<IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> Observers { get; }

    public ObservableInterceptor(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor, params IReadOnlyList<IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
        : base(interceptor)
    {
        Observers = observers.ToValueArray();
    }

    protected override WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateInterceptorInstance(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> innerInterceptor) =>
        new Instance(innerInterceptor, Observers);

    private sealed class Instance(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> innerInterceptor, ValueArray<IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
        : WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(innerInterceptor)
    {
        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem)
        {
            var result = InnerInterceptor.Transform(currentState, previousState, searchSpace, problem);
            foreach (var observer in observers)
            {
                observer.AfterInterception(result, currentState, previousState, searchSpace, problem);
            }
            return result;
        }
    }
}

public interface IInterceptorObserver<TCandidate, in TSearchSpace, in TProblem, in TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    void AfterInterception(TSearchState newState, TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(
    Action<TSearchState, TSearchState, TSearchState?, TSearchSpace, TProblem> afterInterception)
    : IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public void AfterInterception(TSearchState newState, TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem) =>
        afterInterception(newState, currentState, previousState, searchSpace, problem);
}

public static class ObservableInterceptorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState> observer) =>
            new ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, observer);
        public ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(params IReadOnlyList<IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers) =>
            new ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, observers);
        public ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(Action<TSearchState, TSearchState, TSearchState?, TSearchSpace, TProblem> afterInterception) =>
            interceptor.ObserveWith(new ActionInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(afterInterception));
        public ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(Action<TSearchState> afterInterception) =>
            interceptor.ObserveWith(new ActionInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>((newState, _, _, _, _) => afterInterception(newState)));
    }
}

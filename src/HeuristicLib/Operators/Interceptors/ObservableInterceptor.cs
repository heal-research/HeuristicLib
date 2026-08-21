using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : WrappingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public ValueArray<IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> Observers { get; init; }

    public ObservableInterceptor(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, params IReadOnlyList<IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
        : base(childInterceptor)
    {
        Observers = observers.ToValueArray();
    }

    protected override WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor) =>
        new Instance(childInterceptor, Observers);

    private sealed class Instance(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, ValueArray<IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
        : WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childInterceptor)
    {
        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = ChildInterceptor.Transform(currentState, previousState, random, searchSpace, problem);
            foreach (var observer in observers)
            {
                observer.AfterInterception(result, currentState, previousState, searchSpace, problem);
            }
            return result;
        }
    }
}

public static class ObservableInterceptor
{
    public static ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, params IReadOnlyList<IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new(childInterceptor, observers);

    public static ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, Action<TSearchState, TSearchState, TSearchState?, TSearchSpace, TProblem> afterInterception)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new(childInterceptor, new ActionInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(afterInterception));

    public static ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, Action<TSearchState> afterInterception)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new(childInterceptor, new ActionInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>((newState, _, _, _, _) => afterInterception(newState)));
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

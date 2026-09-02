using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Reports every state transformation to its observers and otherwise delegates to the wrapped interceptor.
/// </summary>
/// <remarks>
/// The observers are typed at the search space and problem they were written for, while the interceptor itself stays
/// agnostic so it can be used over any run for its candidate and search state.
/// </remarks>
public sealed record ObservableInterceptor<TCandidate, TObserverSearchSpace, TObserverProblem, TObserverSearchState>
    : WrappingInterceptor<TCandidate>
    where TObserverSearchState : class, ISearchState
    where TObserverSearchSpace : class, ISearchSpace<TCandidate>
    where TObserverProblem : class, IProblem<TCandidate, TObserverSearchSpace>
{
    public ValueArray<IInterceptorObserver<TCandidate, TObserverSearchSpace, TObserverProblem, TObserverSearchState>> Observers { get; init; }

    public ObservableInterceptor(IInterceptor<TCandidate> childInterceptor, params IReadOnlyList<IInterceptorObserver<TCandidate, TObserverSearchSpace, TObserverProblem, TObserverSearchState>> observers)
        : base(childInterceptor)
    {
        Observers = observers.ToValueArray();
    }

    protected override IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> WrapExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> childInterceptor)
    {
        var observers = new IInterceptorObserver<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>[Observers.Count];
        for (var i = 0; i < observers.Length; i++)
        {
            if (Observers[i] is not IInterceptorObserver<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> observer)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} observes {typeof(TObserverSearchSpace).Name} with {typeof(TObserverProblem).Name}, and cannot observe a run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
            }

            observers[i] = observer;
        }

        return new Instance<TRunSearchSpace, TRunProblem, TRunSearchState>(childInterceptor, observers);
    }

    private sealed class Instance<TSearchSpace, TProblem, TObserverSearchState>(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TObserverSearchState> childInterceptor, IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState>[] observers)
        : WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(childInterceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TObserverSearchState : class, ISearchState
    {
        public override TObserverSearchState Transform(TObserverSearchState currentState, TObserverSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
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
    public static ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TObserverSearchState> Create<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(IInterceptor<TCandidate> childInterceptor, params IReadOnlyList<IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TObserverSearchState : class, ISearchState =>
        new(childInterceptor, observers);

    public static ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TObserverSearchState> Create<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(IInterceptor<TCandidate> childInterceptor, Action<TObserverSearchState, TObserverSearchState, TObserverSearchState?, TSearchSpace, TProblem> afterInterception)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TObserverSearchState : class, ISearchState =>
        new(childInterceptor, new ActionInterceptorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(afterInterception));

    /// <summary>Observes the new state only, so the observer is written at the widest search space and problem.</summary>
    public static ObservableInterceptor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TObserverSearchState> Create<TCandidate, TObserverSearchState>(IInterceptor<TCandidate> childInterceptor, Action<TObserverSearchState> afterInterception)
        where TObserverSearchState : class, ISearchState =>
        new(childInterceptor, new ActionInterceptorObserver<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TObserverSearchState>((newState, _, _, _, _) => afterInterception(newState)));
}

public interface IInterceptorObserver<TCandidate, in TSearchSpace, in TProblem, in TObserverSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TObserverSearchState : class, ISearchState
{
    void AfterInterception(TObserverSearchState newState, TObserverSearchState currentState, TObserverSearchState? previousState, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionInterceptorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(
    Action<TObserverSearchState, TObserverSearchState, TObserverSearchState?, TSearchSpace, TProblem> afterInterception)
    : IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TObserverSearchState : class, ISearchState
{
    public void AfterInterception(TObserverSearchState newState, TObserverSearchState currentState, TObserverSearchState? previousState, TSearchSpace searchSpace, TProblem problem) =>
        afterInterception(newState, currentState, previousState, searchSpace, problem);
}

public static class ObservableInterceptorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(IInterceptor<TCandidate> interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TObserverSearchState : class, ISearchState
    {
        public ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TObserverSearchState> ObserveWith(IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState> observer) =>
            new ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(interceptor, observer);
        public ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TObserverSearchState> ObserveWith(params IReadOnlyList<IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState>> observers) =>
            new ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(interceptor, observers);
        public ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TObserverSearchState> ObserveWith(Action<TObserverSearchState, TObserverSearchState, TObserverSearchState?, TSearchSpace, TProblem> afterInterception) =>
            interceptor.ObserveWith(new ActionInterceptorObserver<TCandidate, TSearchSpace, TProblem, TObserverSearchState>(afterInterception));
    }

    extension<TCandidate, TObserverSearchState>(IInterceptor<TCandidate> interceptor)
        where TObserverSearchState : class, ISearchState
    {
        public ObservableInterceptor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TObserverSearchState> ObserveWith(Action<TObserverSearchState> afterInterception) =>
            ObservableInterceptor.Create(interceptor, afterInterception);
    }
}

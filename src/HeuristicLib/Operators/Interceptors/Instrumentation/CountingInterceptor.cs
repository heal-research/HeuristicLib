using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public sealed record CountingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> : ObservableInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public ObservationCounter Counter { get; }

    public CountingInterceptor(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor, ObservationCounter counter)
        : base(interceptor, new ActionInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>((_, _, _, _, _) => counter.IncrementBy(1)))
    {
        Counter = counter;
    }
}

public static class InterceptorCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public CountingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> CountInterceptorCalls(ObservationCounter counter) => new(interceptor, counter);

        public CountingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> CountInterceptorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return interceptor.CountInterceptorCalls(counter);
        }
    }
}

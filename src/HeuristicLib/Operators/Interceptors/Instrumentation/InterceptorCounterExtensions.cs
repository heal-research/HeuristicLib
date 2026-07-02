using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public static class InterceptorCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> CountInterceptorCalls(ObservationCounter counter)
            => interceptor.ObserveWith(_ => counter.IncrementBy(1));

        public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> CountInterceptorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return interceptor.CountInterceptorCalls(counter);
        }
    }
}

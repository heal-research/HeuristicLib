using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public static class InterceptorCounterExtensions
{
    extension<TG, TS, TP, TR>(IInterceptor<TG, TS, TP, TR> interceptor)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
        where TR : class, ISearchState
    {
        public IInterceptor<TG, TS, TP, TR> CountInterceptorCalls(ObservationCounter counter)
            => interceptor.ObserveWith(_ => counter.IncrementBy(1));

        public IInterceptor<TG, TS, TP, TR> CountInterceptorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return interceptor.CountInterceptorCalls(counter);
        }
    }
}

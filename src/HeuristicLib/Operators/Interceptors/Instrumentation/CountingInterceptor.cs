using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public sealed record CountingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : WrappingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public ObservationCounter Counter { get; init; }

    public CountingInterceptor(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, ObservationCounter counter)
        : base(childInterceptor)
    {
        Counter = counter;
    }

    protected override WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor) =>
        new Instance(childInterceptor, Counter);

    private sealed class Instance(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, ObservationCounter counter)
        : WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childInterceptor)
    {
        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var transformedState = ChildInterceptor.Transform(currentState, previousState, random, searchSpace, problem);
            counter.IncrementBy(1);
            return transformedState;
        }
    }
}

public static class CountingInterceptor
{
    public static CountingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, ObservationCounter counter)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new(childInterceptor, counter);
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

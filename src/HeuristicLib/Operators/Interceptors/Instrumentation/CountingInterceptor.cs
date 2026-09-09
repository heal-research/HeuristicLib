using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public sealed record CountingInterceptor<TCandidate>
    : WrappingInterceptor<TCandidate>
{
    public ObservationCounter Counter { get; init; }

    public CountingInterceptor(IInterceptor<TCandidate> childInterceptor, ObservationCounter counter)
        : base(childInterceptor)
    {
        Counter = counter;
    }

    protected override IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> WrapExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> childInterceptor) =>
        new Instance<TRunSearchSpace, TRunProblem, TRunSearchState>(childInterceptor, Counter);

    private sealed class Instance<TSearchSpace, TProblem, TSearchState>(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, ObservationCounter counter)
        : WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childInterceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
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
    public static CountingInterceptor<TCandidate> Create<TCandidate>(IInterceptor<TCandidate> childInterceptor, ObservationCounter counter) =>
        new(childInterceptor, counter);
}

public static class InterceptorCounterExtensions
{
    extension<TCandidate>(IInterceptor<TCandidate> interceptor)
    {
        public CountingInterceptor<TCandidate> CountCalls(ObservationCounter counter) => new(interceptor, counter);

        public CountingInterceptor<TCandidate> CountCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return interceptor.CountCalls(counter);
        }
    }
}

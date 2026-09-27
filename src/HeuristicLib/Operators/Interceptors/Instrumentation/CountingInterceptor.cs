using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public sealed record CountingInterceptor<TCandidate>
    : WrappingInterceptor<TCandidate>
{
    public CountAccumulator Counter { get; init; }

    public CountingInterceptor(IInterceptor<TCandidate> childInterceptor, CountAccumulator counter)
        : base(childInterceptor)
    {
        Counter = counter;
    }

    protected override IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> WrapExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> childInterceptor) =>
        new Execution<TRunSearchSpace, TRunProblem, TRunSearchState>(childInterceptor, Counter);

    private sealed class Execution<TSearchSpace, TProblem, TSearchState>(IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, CountAccumulator counter)
        : WrappingInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>(childInterceptor)
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
    public static CountingInterceptor<TCandidate> Create<TCandidate>(IInterceptor<TCandidate> childInterceptor, CountAccumulator counter) =>
        new(childInterceptor, counter);
}

public static class InterceptorCounterExtensions
{
    extension<TCandidate>(IInterceptor<TCandidate> interceptor)
    {
        public CountingInterceptor<TCandidate> CountCalls(CountAccumulator counter) => new(interceptor, counter);

        public CountingInterceptor<TCandidate> CountCalls(out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return interceptor.CountCalls(counter);
        }
    }
}

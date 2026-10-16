using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

public sealed class CountingInterceptorAdvice<TCandidate> : InterceptorAdvice<TCandidate>
{
    public CountAccumulator Counter { get; }

    public CountingInterceptorAdvice(CountAccumulator counter)
    {
        Counter = counter;
    }

    public override WrapperExecutorFactory<IInterceptionExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem, TRunSearchState>(IInterceptor<TCandidate> interceptor) =>
        childInterceptor => new Executor<TRunSearchSpace, TRunProblem, TRunSearchState>(childInterceptor, Counter);

    private sealed class Executor<TSearchSpace, TProblem, TSearchState>(IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor, CountAccumulator counter)
        : WrappingInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(childInterceptor)
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

public sealed record InterceptorCounting<TCandidate> : IOperatorCounting<IInterceptor<TCandidate>>
{
    public Advice<IInterceptor<TCandidate>> CreateAdvice(CountAccumulator counter) =>
        new CountingInterceptorAdvice<TCandidate>(counter);
}

public static class InterceptorCounterExtensions
{
    extension(Aspect)
    {
        public static Aspect CountCalls<TCandidate>(IInterceptor<TCandidate> interceptor, CountAccumulator counter) =>
            Aspect.Create(Pointcut.Reference(interceptor), new CountingInterceptorAdvice<TCandidate>(counter));
    }
}

using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public sealed record InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Interceptor,
    TSearchState State,
    TSearchState UntransformedState,
    TSearchState? PreviousState,
    TSearchSpace SearchSpace,
    TProblem Problem) : Observation<TProblem>(Problem)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState;

public static class InterceptorObservations
{
    extension(ExecutionInstanceResolverBuilder builder)
    {
        /// <summary>Observes every call to one chosen interceptor.</summary>
        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor,
            Action<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            builder.Install(new InterceptorObservationHook<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, observe));
    }
}

internal sealed class InterceptorObservationHook<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor,
    Action<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe) : IExecutionHook
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public void Install(ExecutionInstanceResolverBuilder builder) =>
        builder.Decorate(interceptor, current => new ObservingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, current, observe));
}

internal sealed class ObservingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> observedInterceptor,
    IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor,
    Action<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe)
    : IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceResolver resolver) =>
        new Instance(observedInterceptor, resolver.Resolve(childInterceptor), observe);

    private sealed class Instance(
        IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> observedInterceptor,
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor,
        Action<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe)
        : IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random,
            TSearchSpace searchSpace, TProblem problem)
        {
            var result = childInterceptor.Transform(currentState, previousState, random, searchSpace, problem);
            observe(new InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>(
                observedInterceptor, result, currentState, previousState, searchSpace, problem));
            return result;
        }
    }
}

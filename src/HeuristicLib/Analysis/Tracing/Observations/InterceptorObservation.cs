using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public sealed record InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IInterceptor<TCandidate> Interceptor,
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
        /// <remarks>
        /// The interceptor names only its candidate, so the observation names the search space, problem and search
        /// state it reads. A run over types the observation was not written for is reported when the execution graph
        /// is built.
        /// </remarks>
        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IInterceptor<TCandidate> interceptor,
            Action<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            builder.Install(new InterceptorObservationHook<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, observe));

        /// <summary>
        /// Observes every call to one chosen interceptor, for an observer that reads no particular search space, problem
        /// or search state.
        /// </summary>
        /// <remarks>
        /// An implicitly typed lambda binds here, because the overload naming the types cannot infer them from it. The
        /// interceptor carries no search state, so the observation reads the states as <see cref="ISearchState"/> and
        /// fits every run over the candidate.
        /// </remarks>
        public void Observe<TCandidate>(
            IInterceptor<TCandidate> interceptor,
            Action<InterceptorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState>> observe) =>
            builder.Observe<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState>(interceptor, observe);
    }

    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(TypedObservationBuilder<TCandidate, TSearchSpace, TProblem, TSearchState> builder)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public void Observe(
            IInterceptor<TCandidate> interceptor,
            Action<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe) =>
            builder.Builder.Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, observe);
    }
}

internal sealed class InterceptorObservationHook<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IInterceptor<TCandidate> interceptor,
    Action<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe) : IExecutionHook
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public void Install(ExecutionInstanceResolverBuilder builder) =>
        builder.Decorate(interceptor, current => new ObservingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, current, observe));
}

internal sealed class ObservingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IInterceptor<TCandidate> observedInterceptor,
    IInterceptor<TCandidate> childInterceptor,
    Action<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe)
    : IInterceptor<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public bool Fits(ExecutionSignature execution) =>
        ObservationSignature.Fits<TSearchSpace, TProblem, TSearchState>(execution) && execution.Fits(childInterceptor);

    public IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ExecutionInstanceResolver resolver)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        ObservationSignature.Require<TSearchSpace, TProblem, TSearchState, TRunSearchSpace, TRunProblem, TRunSearchState>(this);
        return new Instance<TRunSearchSpace, TRunProblem, TRunSearchState>(
            observedInterceptor, resolver.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>(childInterceptor), observe);
    }

    private sealed class Instance<TRunSearchSpace, TRunProblem, TRunSearchState>(
        IInterceptor<TCandidate> observedInterceptor,
        IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> childInterceptor,
        Action<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe)
        : IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public TRunSearchState Transform(TRunSearchState currentState, TRunSearchState? previousState, IRandomNumberGenerator random,
            TRunSearchSpace searchSpace, TRunProblem problem)
        {
            var result = childInterceptor.Transform(currentState, previousState, random, searchSpace, problem);
            observe(new InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>(
                observedInterceptor, (TSearchState)(object)result, (TSearchState)(object)currentState, (TSearchState?)(object?)previousState,
                (TSearchSpace)(object)searchSpace, (TProblem)(object)problem));
            return result;
        }
    }
}

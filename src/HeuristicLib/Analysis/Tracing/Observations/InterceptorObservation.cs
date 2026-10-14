using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Interceptors;
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
    extension(AspectBuilder builder)
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
            builder.Install(new InterceptorObservationAspect<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, observe));

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
}

internal sealed class InterceptorObservationAspect<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IInterceptor<TCandidate> interceptor,
    Action<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe) : Aspect
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    protected override void Declare(AspectBuilder builder) =>
        builder.Install(interceptor, current => new ObservingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, current, observe));
}

internal sealed record ObservingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> : WrappingInterceptor<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IInterceptor<TCandidate> observedInterceptor;
    private readonly Action<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe;

    public ObservingInterceptor(IInterceptor<TCandidate> observedInterceptor, IInterceptor<TCandidate> childInterceptor, Action<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe)
        : base(childInterceptor)
    {
        this.observedInterceptor = observedInterceptor;
        this.observe = observe;
    }

    public override bool Fits(ExecutionSignature execution) =>
        base.Fits(execution) && ObservationSignature.Fits<TSearchSpace, TProblem, TSearchState>(execution);

    protected override WrapperExecutionFactory<IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
    {
        ObservationSignature.Require<TSearchSpace, TProblem, TSearchState, TRunSearchSpace, TRunProblem, TRunSearchState>(this);
        return inner => new Execution<TRunSearchSpace, TRunProblem, TRunSearchState>(observedInterceptor, inner, observe);
    }

    private sealed class Execution<TRunSearchSpace, TRunProblem, TRunSearchState>(
        IInterceptor<TCandidate> observedInterceptor,
        IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> childInterceptor,
        Action<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe)
        : WrappingInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>(childInterceptor)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override TRunSearchState Transform(TRunSearchState currentState, TRunSearchState? previousState, IRandomNumberGenerator random,
            TRunSearchSpace searchSpace, TRunProblem problem)
        {
            var result = ChildInterceptor.Transform(currentState, previousState, random, searchSpace, problem);
            observe(new InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>(
                observedInterceptor, (TSearchState)(object)result, (TSearchState)(object)currentState, (TSearchState?)(object?)previousState,
                (TSearchSpace)(object)searchSpace, (TProblem)(object)problem));
            return result;
        }
    }
}

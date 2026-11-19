using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// One successful interception call, with the search state passed to the interceptor as its untransformed state and the
/// state it returned.
/// </summary>
/// <remarks>
/// The search space and problem are read at the candidate's interfaces, so an observation fits every run over the
/// candidate.
/// </remarks>
public sealed record InterceptionObservation<TCandidate, TSearchState>(
    IOperator<Interception<TSearchState>, TCandidate> Interceptor,
    TSearchState UntransformedState,
    TSearchState? PreviousState,
    TSearchState State,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    : IInvocationObservation<Interception<TSearchState>, TCandidate, InterceptionObservation<TCandidate, TSearchState>>
    where TSearchState : class, ISearchState
{
    static IExecutor IInvocationObservation<Interception<TSearchState>, TCandidate, InterceptionObservation<TCandidate, TSearchState>>.Wrap<TSearchSpace, TProblem, TRunSearchState>(
        IExecutor executor, IOperator<Interception<TSearchState>, TCandidate> source, Action<InterceptionObservation<TCandidate, TSearchState>> record, TProblem problem) =>
        new Executor((IInterceptionExecutor<TSearchState>)executor, source, record, problem);

    private sealed class Executor(IInterceptionExecutor<TSearchState> executor,
        IOperator<Interception<TSearchState>, TCandidate> interceptor, Action<InterceptionObservation<TCandidate, TSearchState>> observe, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        : InterceptionExecutor<TSearchState>
    {
        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random)
        {
            var state = executor.Transform(currentState, previousState, random);
            observe(new InterceptionObservation<TCandidate, TSearchState>(interceptor, currentState, previousState, state, problem.SearchSpace, problem));
            return state;
        }
    }
}

public static class InterceptionObservations
{
    extension<TCandidate, TSearchState>(IOperator<Interception<TSearchState>, TCandidate> interceptor)
        where TSearchState : class, ISearchState
    {
        /// <summary>The observations of this interceptor's successful calls.</summary>
        public InvocationSource<Interception<TSearchState>, TCandidate, InterceptionObservation<TCandidate, TSearchState>> Invocations =>
            ObservationSource.Create<Interception<TSearchState>, TCandidate, InterceptionObservation<TCandidate, TSearchState>>(interceptor);

        /// <summary>Records this interceptor's successful calls into the receiver, wherever this configuration is placed.</summary>
        public Observed<Interception<TSearchState>, TCandidate, InterceptionObservation<TCandidate, TSearchState>> Observed(IObservationReceiver<InterceptionObservation<TCandidate, TSearchState>> receiver) =>
            new(interceptor, receiver);
    }
}

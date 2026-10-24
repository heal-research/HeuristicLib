using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
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
    where TSearchState : class, ISearchState;

file sealed class InterceptionObservationAdvice<TCandidate, TSearchState>(Action<InterceptionObservation<TCandidate, TSearchState>> observe)
    : InterceptorAdvice<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    protected override IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Wrap<TSearchSpace, TProblem>(
        IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, IOperator<Interception<TSearchState>, TCandidate> interceptor) =>
        new Executor<TSearchSpace, TProblem>(executor, interceptor, observe);

    private sealed class Executor<TSearchSpace, TProblem>(IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor,
        IOperator<Interception<TSearchState>, TCandidate> interceptor, Action<InterceptionObservation<TCandidate, TSearchState>> observe)
        : IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>, IExecutorControlProvider
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace,
            TProblem problem)
        {
            var state = executor.Transform(currentState, previousState, random, searchSpace, problem);
            observe(new InterceptionObservation<TCandidate, TSearchState>(interceptor, currentState, previousState, state, searchSpace, problem));
            return state;
        }

        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
    }
}

public static class InterceptionObservations
{
    extension<TCandidate, TSearchState>(IOperator<Interception<TSearchState>, TCandidate> interceptor)
        where TSearchState : class, ISearchState
    {
        /// <summary>The observations of this interceptor's successful calls.</summary>
        public ObservationSource<InterceptionObservation<TCandidate, TSearchState>> Calls =>
            new(interceptor, observe => Aspect.Create(Pointcut.Reference(interceptor),
                new InterceptionObservationAdvice<TCandidate, TSearchState>(observe)));
    }
}

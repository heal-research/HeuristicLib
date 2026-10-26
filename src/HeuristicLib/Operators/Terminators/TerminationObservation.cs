using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>One successful termination call, with the search state it checked and its decision.</summary>
/// <remarks>
/// The search space and problem are read at the candidate's interfaces, so an observation fits every run over the
/// candidate.
/// </remarks>
public sealed record TerminationObservation<TCandidate>(
    IOperator<Termination, TCandidate> Terminator,
    ISearchState State,
    bool IsTerminal,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem);

file sealed class TerminationObservationAdvice<TCandidate>(Action<TerminationObservation<TCandidate>> observe) : TerminatorAdvice<TCandidate>
{
    protected override ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Wrap<TSearchSpace, TProblem, TSearchState>(
        ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, IOperator<Termination, TCandidate> terminator) =>
        new Executor<TSearchSpace, TProblem, TSearchState>(executor, terminator, observe);

    private sealed class Executor<TSearchSpace, TProblem, TSearchState>(ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor,
        IOperator<Termination, TCandidate> terminator, Action<TerminationObservation<TCandidate>> observe)
        : ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>, IExecutorControlProvider
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var isTerminal = executor.IsTerminalState(state, random, searchSpace, problem);
            observe(new TerminationObservation<TCandidate>(terminator, state, isTerminal, searchSpace, problem));
            return isTerminal;
        }

        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
    }
}

public static class TerminationObservations
{
    extension<TCandidate>(IOperator<Termination, TCandidate> terminator)
    {
        /// <summary>The observations of this terminator's successful calls.</summary>
        public ObservationSource<TerminationObservation<TCandidate>> Invocations =>
            ObservationSource.Create<TerminationObservation<TCandidate>>(terminator, observe => Aspect.Create(Pointcut.Reference(terminator), new TerminationObservationAdvice<TCandidate>(observe)));
    }
}

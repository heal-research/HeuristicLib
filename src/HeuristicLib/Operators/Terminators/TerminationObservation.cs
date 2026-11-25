using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Terminators;
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
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    : IInvocationObservation<Termination, TCandidate, TerminationObservation<TCandidate>>
{
    static IExecutor IInvocationObservation<Termination, TCandidate, TerminationObservation<TCandidate>>.Wrap<TSearchState>(
        IExecutor executor, IOperator<Termination, TCandidate> source, Action<TerminationObservation<TCandidate>> record,
        IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        new Executor<TSearchState>((ITerminationExecutor<TSearchState>)executor, source, record, problem);

    private sealed class Executor<TSearchState>(ITerminationExecutor<TSearchState> executor,
        IOperator<Termination, TCandidate> terminator, Action<TerminationObservation<TCandidate>> observe, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        : TerminationExecutor<TSearchState>
        where TSearchState : class, ISearchState
    {
        public override bool IsTerminalState(TSearchState state, IRandomNumberGenerator random)
        {
            var isTerminal = executor.IsTerminalState(state, random);
            observe(new TerminationObservation<TCandidate>(terminator, state, isTerminal, problem.SearchSpace, problem));
            return isTerminal;
        }
    }
}

public static class TerminationObservations
{
    extension<TCandidate>(IOperator<Termination, TCandidate> terminator)
    {
        /// <summary>The observations of this terminator's successful calls.</summary>
        public InvocationSource<Termination, TCandidate, TerminationObservation<TCandidate>> Invocations =>
            ObservationSource.Create<Termination, TCandidate, TerminationObservation<TCandidate>>(terminator);

        /// <summary>Records this terminator's successful calls into the receiver, wherever this configuration is placed.</summary>
        public Observed<Termination, TCandidate, TerminationObservation<TCandidate>> Observed(IObservationReceiver<TerminationObservation<TCandidate>> receiver) =>
            new(terminator, receiver);
    }
}

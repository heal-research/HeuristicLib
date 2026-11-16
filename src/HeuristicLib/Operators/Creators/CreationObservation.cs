using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>One successful creation call, with the requested count and the candidates it returned.</summary>
/// <remarks>
/// The search space and problem are read at the candidate's interfaces, so an observation fits every run over the
/// candidate.
/// </remarks>
public sealed record CreationObservation<TCandidate>(
    IOperator<Creation, TCandidate> Creator,
    int Count,
    IReadOnlyList<TCandidate> Candidates,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    : IInvocationObservation<Creation, TCandidate, CreationObservation<TCandidate>>
{
    static IExecutor IInvocationObservation<Creation, TCandidate, CreationObservation<TCandidate>>.Wrap<TSearchSpace, TProblem, TSearchState>(
        IExecutor executor, IOperator<Creation, TCandidate> source, Action<CreationObservation<TCandidate>> record, TProblem problem) =>
        new Executor<TSearchSpace, TProblem>((ICreationExecutor<TCandidate>)executor, source, record, problem);

    private sealed class Executor<TSearchSpace, TProblem>(ICreationExecutor<TCandidate> executor,
        IOperator<Creation, TCandidate> creator, Action<CreationObservation<TCandidate>> observe, TProblem problem)
        : CreationExecutor<TCandidate, TSearchSpace, TProblem>(problem), IExecutorControlProvider
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random)
        {
            var candidates = executor.Create(count, random);
            observe(new CreationObservation<TCandidate>(creator, count, candidates, SearchSpace, Problem));
            return candidates;
        }

        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
    }
}

public static class CreationObservations
{
    extension<TCandidate>(IOperator<Creation, TCandidate> creator)
    {
        /// <summary>The observations of this creator's successful calls.</summary>
        public InvocationSource<Creation, TCandidate, CreationObservation<TCandidate>> Invocations =>
            ObservationSource.Create<Creation, TCandidate, CreationObservation<TCandidate>>(creator);

        /// <summary>Records this creator's successful calls into the instrument, wherever this configuration is placed.</summary>
        public Observed<Creation, TCandidate, CreationObservation<TCandidate>> Observed(IObservationInstrument<CreationObservation<TCandidate>> instrument) =>
            new(creator, instrument);
    }
}

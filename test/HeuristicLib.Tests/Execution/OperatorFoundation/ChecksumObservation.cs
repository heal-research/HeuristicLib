using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

// An external operation becomes observable through the same parts a built-in operation declares: its record, which makes
// an executor report it, its source and its explicit observation wrapper.
public sealed record ChecksumObservation<TCandidate>(
    IOperator<Checksum, TCandidate> Checksum,
    int Result,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    : IInvocationObservation<Checksum, TCandidate, ChecksumObservation<TCandidate>>
{
    static IExecutor IInvocationObservation<Checksum, TCandidate, ChecksumObservation<TCandidate>>.Wrap<TSearchSpace, TProblem, TSearchState>(
        IExecutor executor, IOperator<Checksum, TCandidate> source, Action<ChecksumObservation<TCandidate>> record) =>
        new Executor<TSearchSpace, TProblem>((IChecksumExecutor<TCandidate, TSearchSpace, TProblem>)executor, source, record);

    private sealed class Executor<TSearchSpace, TProblem>(IChecksumExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IOperator<Checksum, TCandidate> checksum, Action<ChecksumObservation<TCandidate>> observe)
        : IChecksumExecutor<TCandidate, TSearchSpace, TProblem>, IExecutorControlProvider
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public int Read(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = executor.Read(random, searchSpace, problem);
            observe(new ChecksumObservation<TCandidate>(checksum, result, searchSpace, problem));
            return result;
        }

        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
    }
}

public static class ChecksumObservations
{
    extension<TCandidate>(IOperator<Checksum, TCandidate> checksum)
    {
        public InvocationSource<Checksum, TCandidate, ChecksumObservation<TCandidate>> Invocations =>
            ObservationSource.Create<Checksum, TCandidate, ChecksumObservation<TCandidate>>(checksum);

        public Observed<Checksum, TCandidate, ChecksumObservation<TCandidate>> Observed(IObservationInstrument<ChecksumObservation<TCandidate>> instrument) =>
            new(checksum, instrument);
    }
}

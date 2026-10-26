using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

// An external operation becomes observable through the same three parts a built-in operation declares: its record,
// advice and source.
public sealed record ChecksumObservation<TCandidate>(
    IOperator<Checksum, TCandidate> Checksum,
    int Result,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem);

file sealed class ChecksumObservationAdvice<TCandidate>(Action<ChecksumObservation<TCandidate>> observe)
    : OperatorAdvice<Checksum, TCandidate>
{
    public override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor, OperationInfo operation,
        IOperator<Checksum, TCandidate> source) =>
        new Executor<TSearchSpace, TProblem>((IChecksumExecutor<TCandidate, TSearchSpace, TProblem>)executor, source, observe);

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
        public ObservationSource<ChecksumObservation<TCandidate>> Invocations =>
            ObservationSource.Create<ChecksumObservation<TCandidate>>(checksum, observe => Aspect.Create(Pointcut.Reference(checksum), new ChecksumObservationAdvice<TCandidate>(observe)));
    }
}

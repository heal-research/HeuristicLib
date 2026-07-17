using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public sealed record CountingMutator<TCandidate, TSearchSpace, TProblem> : ObservableMutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationCounter Counter { get; }
    public OperatorCountMetric Metric { get; }

    public CountingMutator(IMutator<TCandidate, TSearchSpace, TProblem> mutator, ObservationCounter counter, OperatorCountMetric metric)
        : base(mutator, new ActionMutatorObserver<TCandidate, TSearchSpace, TProblem>(
            (offspring, _, _, _) => counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : offspring.Count)))
    {
        Counter = counter;
        Metric = metric;
    }
}

public static class MutatorCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public CountingMutator<TCandidate, TSearchSpace, TProblem> CountMutatorCalls(ObservationCounter counter) => new(mutator, counter, OperatorCountMetric.Calls);

        public CountingMutator<TCandidate, TSearchSpace, TProblem> CountMutatorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return mutator.CountMutatorCalls(counter);
        }

        public CountingMutator<TCandidate, TSearchSpace, TProblem> CountMutatedCandidates(ObservationCounter counter) => new(mutator, counter, OperatorCountMetric.Candidates);

        public CountingMutator<TCandidate, TSearchSpace, TProblem> CountMutatedCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return mutator.CountMutatedCandidates(counter);
        }
    }
}

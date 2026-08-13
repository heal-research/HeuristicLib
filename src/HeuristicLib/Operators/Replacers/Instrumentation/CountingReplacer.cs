using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public sealed record CountingReplacer<TCandidate, TSearchSpace, TProblem> : ObservableReplacer<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationCounter Counter { get; init; }
    public OperatorCountMetric Metric { get; init; }

    public CountingReplacer(IReplacer<TCandidate, TSearchSpace, TProblem> replacer, ObservationCounter counter, OperatorCountMetric metric)
        : base(replacer, new ActionReplacerObserver<TCandidate, TSearchSpace, TProblem>(
            (newPopulation, _, _, _, _, _) => counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : newPopulation.Count)))
    {
        Counter = counter;
        Metric = metric;
    }
}

public static class ReplacerCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate, TSearchSpace, TProblem> replacer)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public CountingReplacer<TCandidate, TSearchSpace, TProblem> CountReplacerCalls(ObservationCounter counter) => new(replacer, counter, OperatorCountMetric.Calls);

        public CountingReplacer<TCandidate, TSearchSpace, TProblem> CountReplacerCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return replacer.CountReplacerCalls(counter);
        }

        public CountingReplacer<TCandidate, TSearchSpace, TProblem> CountReplacementCandidates(ObservationCounter counter) =>
            new(replacer, counter, OperatorCountMetric.Candidates);

        public CountingReplacer<TCandidate, TSearchSpace, TProblem> CountReplacementCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return replacer.CountReplacementCandidates(counter);
        }
    }
}

using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public sealed record CountingSelector<TCandidate, TSearchSpace, TProblem> : ObservableSelector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ObservationCounter Counter { get; }
    public OperatorCountMetric Metric { get; }

    public CountingSelector(ISelector<TCandidate, TSearchSpace, TProblem> selector, ObservationCounter counter, OperatorCountMetric metric)
        : base(selector, new ActionSelectorObserver<TCandidate, TSearchSpace, TProblem>(
            (selected, _, _, _, _, _) => counter.IncrementBy(metric == OperatorCountMetric.Calls ? 1 : selected.Count)))
    {
        Counter = counter;
        Metric = metric;
    }
}

public static class SelectorCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> selector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public CountingSelector<TCandidate, TSearchSpace, TProblem> CountSelectorCalls(ObservationCounter counter) => new(selector, counter, OperatorCountMetric.Calls);

        public CountingSelector<TCandidate, TSearchSpace, TProblem> CountSelectorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return selector.CountSelectorCalls(counter);
        }

        public CountingSelector<TCandidate, TSearchSpace, TProblem> CountSelectedCandidates(ObservationCounter counter) => new(selector, counter, OperatorCountMetric.Candidates);

        public CountingSelector<TCandidate, TSearchSpace, TProblem> CountSelectedCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return selector.CountSelectedCandidates(counter);
        }
    }
}

using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public static class SelectorCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> selector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ISelector<TCandidate, TSearchSpace, TProblem> CountSelectorCalls(ObservationCounter counter)
            => selector.ObserveWith(_ => counter.IncrementBy(1));

        public ISelector<TCandidate, TSearchSpace, TProblem> CountSelectorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return selector.CountSelectorCalls(counter);
        }

        public ISelector<TCandidate, TSearchSpace, TProblem> CountSelectedCandidates(ObservationCounter counter)
            => selector.ObserveWith(selected => counter.IncrementBy(selected.Count));

        public ISelector<TCandidate, TSearchSpace, TProblem> CountSelectedCandidates(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return selector.CountSelectedCandidates(counter);
        }
    }
}

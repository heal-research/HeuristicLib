using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public static class SelectorCounterExtensions
{
    extension<TG, TS, TP>(ISelector<TG, TS, TP> selector)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
    {
        public ISelector<TG, TS, TP> CountSelectorCalls(ObservationCounter counter)
            => selector.ObserveWith(_ => counter.IncrementBy(1));

        public ISelector<TG, TS, TP> CountSelectorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return selector.CountSelectorCalls(counter);
        }

        public ISelector<TG, TS, TP> CountSelectedSolutions(ObservationCounter counter)
            => selector.ObserveWith(selected => counter.IncrementBy(selected.Count));

        public ISelector<TG, TS, TP> CountSelectedSolutions(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return selector.CountSelectedSolutions(counter);
        }
    }
}

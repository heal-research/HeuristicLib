using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public static class TerminatorCounterExtensions
{
    extension<TG, TS, TP, TR>(ITerminator<TG, TS, TP, TR> terminator)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
        where TR : class, ISearchState
    {
        public ITerminator<TG, TS, TP, TR> CountTerminatorCalls(ObservationCounter counter)
            => terminator.ObserveWith(_ => counter.IncrementBy(1));

        public ITerminator<TG, TS, TP, TR> CountTerminatorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return terminator.CountTerminatorCalls(counter);
        }
    }
}

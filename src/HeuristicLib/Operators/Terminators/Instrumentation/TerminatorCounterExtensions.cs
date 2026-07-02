using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public static class TerminatorCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> CountTerminatorCalls(ObservationCounter counter)
            => terminator.ObserveWith(_ => counter.IncrementBy(1));

        public ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> CountTerminatorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return terminator.CountTerminatorCalls(counter);
        }
    }
}

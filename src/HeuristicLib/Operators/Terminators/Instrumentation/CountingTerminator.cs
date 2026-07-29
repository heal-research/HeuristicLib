using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public sealed record CountingTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> : ObservableTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public ObservationCounter Counter { get; }

    public CountingTerminator(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator, ObservationCounter counter)
        : base(terminator, new ActionTerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>((_, _, _, _) => counter.IncrementBy(1)))
    {
        Counter = counter;
    }
}

public static class TerminatorCounterExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public CountingTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> CountTerminatorCalls(ObservationCounter counter) => new(terminator, counter);

        public CountingTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> CountTerminatorCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return terminator.CountTerminatorCalls(counter);
        }
    }
}

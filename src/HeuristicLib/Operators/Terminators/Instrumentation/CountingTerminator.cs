using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

public sealed record CountingTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    : WrappingTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public ObservationCounter Counter { get; init; }

    public CountingTerminator(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, ObservationCounter counter)
        : base(childTerminator)
    {
        Counter = counter;
    }

    protected override WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator) =>
        new Instance(childTerminator, Counter);

    private sealed class Instance(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, ObservationCounter counter)
        : WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childTerminator)
    {
        public override bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem)
        {
            var result = ChildTerminator.IsTerminalState(state, searchSpace, problem);
            counter.IncrementBy(1);
            return result;
        }
    }
}

public static class CountingTerminator
{
    public static CountingTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, ObservationCounter counter)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new(childTerminator, counter);
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

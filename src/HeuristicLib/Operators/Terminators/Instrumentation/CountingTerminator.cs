using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

public sealed record CountingTerminator<TCandidate>
    : WrappingTerminator<TCandidate>
{
    public ObservationCounter Counter { get; init; }

    public CountingTerminator(ITerminator<TCandidate> childTerminator, ObservationCounter counter)
        : base(childTerminator)
    {
        Counter = counter;
    }

    protected override ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> WrapExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> childTerminator) =>
        new Instance<TRunSearchSpace, TRunProblem, TRunSearchState>(childTerminator, Counter);

    private sealed class Instance<TSearchSpace, TProblem, TSearchState>(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, ObservationCounter counter)
        : WrappingTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childTerminator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
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
    public static CountingTerminator<TCandidate> Create<TCandidate>(ITerminator<TCandidate> childTerminator, ObservationCounter counter) =>
        new(childTerminator, counter);
}

public static class TerminatorCounterExtensions
{
    extension<TCandidate>(ITerminator<TCandidate> terminator)
    {
        public CountingTerminator<TCandidate> CountCalls(ObservationCounter counter) => new(terminator, counter);

        public CountingTerminator<TCandidate> CountCalls(out ObservationCounter counter)
        {
            counter = new ObservationCounter();
            return terminator.CountCalls(counter);
        }
    }
}

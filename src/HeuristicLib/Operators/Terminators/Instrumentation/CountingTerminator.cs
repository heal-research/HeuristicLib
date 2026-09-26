using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

public sealed record CountingTerminator<TCandidate>
    : WrappingTerminator<TCandidate>
{
    public CountAccumulator Counter { get; init; }

    public CountingTerminator(ITerminator<TCandidate> childTerminator, CountAccumulator counter)
        : base(childTerminator)
    {
        Counter = counter;
    }

    protected override ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> WrapExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> childTerminator) =>
        new Instance<TRunSearchSpace, TRunProblem, TRunSearchState>(childTerminator, Counter);

    private sealed class Instance<TSearchSpace, TProblem, TSearchState>(ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childTerminator, CountAccumulator counter)
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
    public static CountingTerminator<TCandidate> Create<TCandidate>(ITerminator<TCandidate> childTerminator, CountAccumulator counter) =>
        new(childTerminator, counter);
}

public static class TerminatorCounterExtensions
{
    extension<TCandidate>(ITerminator<TCandidate> terminator)
    {
        public CountingTerminator<TCandidate> CountCalls(CountAccumulator counter) => new(terminator, counter);

        public CountingTerminator<TCandidate> CountCalls(out CountAccumulator counter)
        {
            counter = new CountAccumulator();
            return terminator.CountCalls(counter);
        }
    }
}

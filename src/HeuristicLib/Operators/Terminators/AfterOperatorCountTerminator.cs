using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record AfterOperatorCountTerminator<TCandidate> : StatelessTerminator<TCandidate>
{
    public AfterOperatorCountTerminator(CountAccumulator counter, int maximumCount)
    {
        Counter = counter;
        MaximumCount = maximumCount;
    }

    public CountAccumulator Counter { get; init; }

    public int MaximumCount { get; init; }

    public override bool IsTerminalState()
    {
        return Counter.CurrentCount >= MaximumCount;
    }
}

public static class AfterOperatorCountTerminator
{
    public static AfterOperatorCountTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, CountAccumulator counter, int maximumCount) => new(counter, maximumCount);
}

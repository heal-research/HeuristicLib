using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record AfterOperatorCountTerminator<TCandidate> : StatelessTerminator<TCandidate>
{
    public AfterOperatorCountTerminator(ObservationCounter counter, int maximumCount)
    {
        Counter = counter;
        MaximumCount = maximumCount;
    }

    public ObservationCounter Counter { get; init; }

    public int MaximumCount { get; init; }

    public override bool IsTerminalState()
    {
        return Counter.CurrentCount >= MaximumCount;
    }
}

public static class AfterOperatorCountTerminator
{
    public static AfterOperatorCountTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, ObservationCounter counter, int maximumCount) => new(counter, maximumCount);
}

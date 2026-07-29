using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

public record AfterOperatorCountTerminator<TCandidate> : StatelessTerminator<TCandidate>
{
    public AfterOperatorCountTerminator(ObservationCounter counter, int maximumCount)
    {
        Counter = counter;
        MaximumCount = maximumCount;
    }

    public ObservationCounter Counter { get; }

    public int MaximumCount
    {
        get;
        init => field = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MaximumCount), "MaximumCount must be positive.");
    }

    public override bool IsTerminalState()
    {
        return Counter.CurrentCount >= MaximumCount;
    }
}

public static class AfterOperatorCountTerminator
{
    public static AfterOperatorCountTerminator<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem, ObservationCounter counter, int maximumCount)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new(counter, maximumCount);
}

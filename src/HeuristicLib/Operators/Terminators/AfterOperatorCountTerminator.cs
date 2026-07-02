using HEAL.HeuristicLib.Analysis;

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

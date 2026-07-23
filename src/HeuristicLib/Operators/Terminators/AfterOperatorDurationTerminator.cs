using HEAL.HeuristicLib.Analysis;

namespace HEAL.HeuristicLib.Operators.Terminators;

public record AfterOperatorDurationTerminator<TCandidate> : StatelessTerminator<TCandidate>
{
    public AfterOperatorDurationTerminator(ObservationDuration duration, TimeSpan maximumDuration)
    {
        Duration = duration;
        MaximumDuration = maximumDuration;
    }

    public ObservationDuration Duration { get; }

    public TimeSpan MaximumDuration
    {
        get;
        init => field = value > TimeSpan.Zero
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MaximumDuration), "MaximumDuration must be positive.");
    }

    public override bool IsTerminalState()
    {
        return Duration.CurrentDuration >= MaximumDuration;
    }
}

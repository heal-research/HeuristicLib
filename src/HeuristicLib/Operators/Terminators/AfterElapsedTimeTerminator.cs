namespace HEAL.HeuristicLib.Operators.Terminators;

public record AfterElapsedTimeTerminator<TCandidate>
  : StatefulTerminator<TCandidate, AfterElapsedTimeTerminator<TCandidate>.ExecutionState>
{
    public AfterElapsedTimeTerminator(TimeSpan maximumElapsedTime)
      : this(maximumElapsedTime, TimeProvider.System)
    {
    }

    public AfterElapsedTimeTerminator(TimeSpan maximumElapsedTime, TimeProvider timeProvider)
    {
        MaximumElapsedTime = maximumElapsedTime;
        TimeProvider = timeProvider;
    }

    public sealed class ExecutionState
    {
        public required long StartTimestamp { get; init; }
    }

    public TimeSpan MaximumElapsedTime
    {
        get;
        init => field = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(MaximumElapsedTime), "MaximumElapsedTime must be positive.");
    }

    public TimeProvider TimeProvider { get; }

    protected override ExecutionState CreateInitialState()
    {
        return new ExecutionState
        {
            StartTimestamp = TimeProvider.GetTimestamp()
        };
    }

    protected override bool IsTerminalState(ExecutionState executionState)
    {
        return TimeProvider.GetElapsedTime(executionState.StartTimestamp) >= MaximumElapsedTime;
    }
}

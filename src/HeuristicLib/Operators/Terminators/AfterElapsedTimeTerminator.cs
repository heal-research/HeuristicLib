using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

public sealed record AfterElapsedTimeTerminator<TCandidate>
  : StatefulTerminator<TCandidate, AfterElapsedTimeTerminator<TCandidate>.ExecutionState>
{
    public AfterElapsedTimeTerminator(TimeSpan maximumElapsedTime)
    {
        MaximumElapsedTime = maximumElapsedTime;
    }

    public sealed class ExecutionState
    {
        public required long StartTimestamp { get; init; }
    }

    /// <summary>
    /// Gets the elapsed-time limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit terminates on the first checked state.</remarks>
    public TimeSpan MaximumElapsedTime { get; init; }

    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

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

public static class AfterElapsedTimeTerminator
{
    public static AfterElapsedTimeTerminator<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem, TimeSpan maximumElapsedTime, TimeProvider? timeProvider = null)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new(maximumElapsedTime) { TimeProvider = timeProvider ?? TimeProvider.System };
}

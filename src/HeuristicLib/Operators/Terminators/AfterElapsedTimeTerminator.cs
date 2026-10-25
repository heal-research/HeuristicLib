using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record AfterElapsedTimeTerminator<TCandidate>
    : StatefulTerminator<TCandidate, AfterElapsedTimeTerminator<TCandidate>.State>
{
    public AfterElapsedTimeTerminator(TimeSpan maximumElapsedTime)
    {
        MaximumElapsedTime = maximumElapsedTime;
    }

    public sealed class State
    {
        public required long StartTimestamp { get; init; }
    }

    /// <summary>
    /// Gets the elapsed-time limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit terminates on the first checked state.</remarks>
    public TimeSpan MaximumElapsedTime { get; init; }

    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    protected override State CreateScopedState() =>
        new() { StartTimestamp = TimeProvider.GetTimestamp() };

    protected override bool IsTerminalState(State state) =>
        TimeProvider.GetElapsedTime(state.StartTimestamp) >= MaximumElapsedTime;
}

public static class AfterElapsedTimeTerminator
{
    public static AfterElapsedTimeTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, TimeSpan maximumElapsedTime, TimeProvider? timeProvider = null) => new(maximumElapsedTime) { TimeProvider = timeProvider ?? TimeProvider.System };
}

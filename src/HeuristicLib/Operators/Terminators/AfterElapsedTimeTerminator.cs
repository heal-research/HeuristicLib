using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record AfterElapsedTimeTerminator<TCandidate>
    : Terminator<TCandidate>
{
    public AfterElapsedTimeTerminator(TimeSpan maximumElapsedTime)
    {
        MaximumElapsedTime = maximumElapsedTime;
    }

    /// <summary>
    /// Gets the elapsed-time limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit terminates on the first checked state.</remarks>
    public TimeSpan MaximumElapsedTime { get; init; }

    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    protected override TerminationExecutor<TCandidate> CreateExecutor(ResolutionScope scope) => new Executor(MaximumElapsedTime, TimeProvider);

    private sealed class Executor(TimeSpan maximumElapsedTime, TimeProvider timeProvider) : TerminationExecutor<TCandidate>
    {
        private readonly long startTimestamp = timeProvider.GetTimestamp();

        public override bool IsTerminalState() => timeProvider.GetElapsedTime(startTimestamp) >= maximumElapsedTime;
    }
}

public static class AfterElapsedTimeTerminator
{
    public static AfterElapsedTimeTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, TimeSpan maximumElapsedTime, TimeProvider? timeProvider = null) => new(maximumElapsedTime) { TimeProvider = timeProvider ?? TimeProvider.System };
}

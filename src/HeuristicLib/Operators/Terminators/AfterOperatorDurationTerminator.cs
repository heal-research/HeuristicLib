using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Terminates once the run's result of a duration reaches a maximum duration.</summary>
/// <remarks>
/// The duration measures wherever it is attached or placed in the run, such as <c>evaluator.Invocations.Duration()</c>
/// attached with <c>Advised</c> or placed with <c>Timed</c>, and this terminator reads the same result, whichever is
/// built first. The duration accumulates over the whole run, but the terminator stops only the stream it is placed on;
/// place it on the root algorithm to stop the run.
/// </remarks>
public sealed record AfterOperatorDurationTerminator<TCandidate> : Terminator<TCandidate>
{
    public AfterOperatorDurationTerminator(Duration duration, TimeSpan maximumDuration)
    {
        Duration = duration;
        MaximumDuration = maximumDuration;
    }

    public Duration Duration { get; init; }

    public TimeSpan MaximumDuration { get; init; }

    protected override TerminationExecutor<TCandidate> CreateExecutor(ResolutionScope scope) =>
        new Executor(scope.Results.Result(Duration), MaximumDuration);

    private sealed class Executor(DurationAccumulator duration, TimeSpan maximumDuration) : TerminationExecutor<TCandidate>
    {
        public override bool IsTerminalState() => duration.CurrentDuration >= maximumDuration;
    }
}

public static class AfterOperatorDurationTerminator
{
    public static AfterOperatorDurationTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, Duration duration, TimeSpan maximumDuration) => new(duration, maximumDuration);
}

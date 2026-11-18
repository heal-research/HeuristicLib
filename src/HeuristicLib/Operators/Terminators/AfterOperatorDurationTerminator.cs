using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Terminates once the run's result of a duration reaches a maximum duration.</summary>
/// <remarks>
/// The duration measures wherever it is attached or placed in the run, such as <c>evaluator.Invocations.Timing(duration)</c>
/// attached with <c>CreateRun</c> or placed with <c>Timed</c>, and this terminator reads the same result, whichever is
/// built first. The duration accumulates over the whole run, but the terminator stops only the stream it is placed on;
/// place it on the root algorithm to stop the run.
/// </remarks>
public sealed record AfterOperatorDurationTerminator<TCandidate> : Terminator<TCandidate>
{
    public AfterOperatorDurationTerminator(IConfigurationNode<DurationAccumulator> duration, TimeSpan maximumDuration)
    {
        Duration = duration;
        MaximumDuration = maximumDuration;
    }

    public IConfigurationNode<DurationAccumulator> Duration { get; init; }

    public TimeSpan MaximumDuration { get; init; }

    public static Parameter<AfterOperatorDurationTerminator<TCandidate>, IConfigurationNode<DurationAccumulator>> DurationParameter { get; } =
        new(nameof(Duration), static terminator => terminator.Duration, static (terminator, value) => terminator with { Duration = value });

    public static Parameter<AfterOperatorDurationTerminator<TCandidate>, TimeSpan> MaximumDurationParameter { get; } =
        new(nameof(MaximumDuration), static terminator => terminator.MaximumDuration, static (terminator, value) => terminator with { MaximumDuration = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [DurationParameter, MaximumDurationParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override TerminationExecutor CreateExecutor(ResolutionScope<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState> scope) =>
        new Executor(scope.Resolve(Duration), MaximumDuration);

    private sealed class Executor(DurationAccumulator duration, TimeSpan maximumDuration) : TerminationExecutor
    {
        public override bool IsTerminalState() => duration.CurrentDuration >= maximumDuration;
    }
}

public static class AfterOperatorDurationTerminator
{
    public static AfterOperatorDurationTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, IConfigurationNode<DurationAccumulator> duration, TimeSpan maximumDuration) => new(duration, maximumDuration);
}

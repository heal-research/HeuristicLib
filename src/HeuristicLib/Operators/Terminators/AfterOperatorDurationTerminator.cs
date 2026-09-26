using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record AfterOperatorDurationTerminator<TCandidate> : StatelessTerminator<TCandidate>
{
    public AfterOperatorDurationTerminator(DurationAccumulator duration, TimeSpan maximumDuration)
    {
        Duration = duration;
        MaximumDuration = maximumDuration;
    }

    public DurationAccumulator Duration { get; init; }

    public TimeSpan MaximumDuration { get; init; }

    public override bool IsTerminalState()
    {
        return Duration.CurrentDuration >= MaximumDuration;
    }
}

public static class AfterOperatorDurationTerminator
{
    public static AfterOperatorDurationTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, DurationAccumulator duration, TimeSpan maximumDuration) => new(duration, maximumDuration);
}

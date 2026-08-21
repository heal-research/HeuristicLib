using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record AfterOperatorDurationTerminator<TCandidate> : StatelessTerminator<TCandidate>
{
    public AfterOperatorDurationTerminator(ObservationDuration duration, TimeSpan maximumDuration)
    {
        Duration = duration;
        MaximumDuration = maximumDuration;
    }

    public ObservationDuration Duration { get; init; }

    public TimeSpan MaximumDuration { get; init; }

    public override bool IsTerminalState()
    {
        return Duration.CurrentDuration >= MaximumDuration;
    }
}

public static class AfterOperatorDurationTerminator
{
    public static AfterOperatorDurationTerminator<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem, ObservationDuration duration, TimeSpan maximumDuration)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new(duration, maximumDuration);
}

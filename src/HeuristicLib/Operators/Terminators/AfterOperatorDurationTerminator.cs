using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record AfterOperatorDurationTerminator<TCandidate> : Terminator<TCandidate>
{
    public AfterOperatorDurationTerminator(ResourceHandle<DurationAccumulator> duration, TimeSpan maximumDuration)
    {
        Duration = duration;
        MaximumDuration = maximumDuration;
    }

    public ResourceHandle<DurationAccumulator> Duration { get; init; }

    public TimeSpan MaximumDuration { get; init; }

    protected override TerminationExecutor<TCandidate> CreateExecutor(ResolutionScope scope) =>
        new Executor(scope.Resource(Duration), MaximumDuration);

    private sealed class Executor(DurationAccumulator duration, TimeSpan maximumDuration) : TerminationExecutor<TCandidate>
    {
        public override bool IsTerminalState() => duration.CurrentDuration >= maximumDuration;
    }
}

public static class AfterOperatorDurationTerminator
{
    public static AfterOperatorDurationTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, ResourceHandle<DurationAccumulator> duration, TimeSpan maximumDuration) => new(duration, maximumDuration);
}

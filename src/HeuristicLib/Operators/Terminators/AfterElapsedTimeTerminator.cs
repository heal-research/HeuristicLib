using HEAL.HeuristicLib.Algorithms;
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
    /// Gets the elapsed-time limit measured from executor construction with the run's time provider. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit terminates on the first checked state.</remarks>
    public TimeSpan MaximumElapsedTime { get; init; }

    public static Parameter<AfterElapsedTimeTerminator<TCandidate>, TimeSpan> MaximumElapsedTimeParameter { get; } =
        new(nameof(MaximumElapsedTime), static terminator => terminator.MaximumElapsedTime, static (terminator, value) => terminator with { MaximumElapsedTime = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [MaximumElapsedTimeParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override TerminationExecutor CreateExecutor(ResolutionScope<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState> scope) => new Executor(MaximumElapsedTime, scope.Results.TimeProvider);

    private sealed class Executor(TimeSpan maximumElapsedTime, TimeProvider timeProvider) : TerminationExecutor
    {
        private readonly long startTimestamp = timeProvider.GetTimestamp();

        public override bool IsTerminalState() => timeProvider.GetElapsedTime(startTimestamp) >= maximumElapsedTime;
    }
}

public static class AfterElapsedTimeTerminator
{
    /// <summary>Creates a terminator measuring elapsed time from executor construction with the run's time provider.</summary>
    public static AfterElapsedTimeTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, TimeSpan maximumElapsedTime) => new(maximumElapsedTime);
}

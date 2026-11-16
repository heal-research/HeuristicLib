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
    /// Gets the elapsed-time limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit terminates on the first checked state.</remarks>
    public TimeSpan MaximumElapsedTime { get; init; }

    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    public static Parameter<AfterElapsedTimeTerminator<TCandidate>, TimeSpan> MaximumElapsedTimeParameter { get; } =
        new(nameof(MaximumElapsedTime), static terminator => terminator.MaximumElapsedTime, static (terminator, value) => terminator with { MaximumElapsedTime = value });

    public static Parameter<AfterElapsedTimeTerminator<TCandidate>, TimeProvider> TimeProviderParameter { get; } =
        new(nameof(TimeProvider), static terminator => terminator.TimeProvider, static (terminator, value) => terminator with { TimeProvider = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [MaximumElapsedTimeParameter, TimeProviderParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override TerminationExecutor CreateExecutor(ResolutionScope<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState> scope) => new Executor(MaximumElapsedTime, TimeProvider);

    private sealed class Executor(TimeSpan maximumElapsedTime, TimeProvider timeProvider) : TerminationExecutor
    {
        private readonly long startTimestamp = timeProvider.GetTimestamp();

        public override bool IsTerminalState() => timeProvider.GetElapsedTime(startTimestamp) >= maximumElapsedTime;
    }
}

public static class AfterElapsedTimeTerminator
{
    public static AfterElapsedTimeTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, TimeSpan maximumElapsedTime, TimeProvider? timeProvider = null) => new(maximumElapsedTime) { TimeProvider = timeProvider ?? TimeProvider.System };
}

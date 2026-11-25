using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record AfterIterationsTerminator<TCandidate>
    : Terminator<TCandidate>
{
    public AfterIterationsTerminator(int maximumIterations)
    {
        MaximumIterations = maximumIterations;
    }

    /// <summary>
    /// Gets the iteration limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit terminates on the first checked state.</remarks>
    public int MaximumIterations { get; init; }

    public static Parameter<AfterIterationsTerminator<TCandidate>, int> MaximumIterationsParameter { get; } =
        new(nameof(MaximumIterations), static terminator => terminator.MaximumIterations, static (terminator, value) => terminator with { MaximumIterations = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [MaximumIterationsParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override TerminationExecutor CreateExecutor(ResolutionScope<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState> scope) => new Executor(MaximumIterations);

    private sealed class Executor(int maximumIterations) : TerminationExecutor
    {
        private int currentCount;

        public override bool IsTerminalState() => ++currentCount >= maximumIterations;
    }
}

public static class AfterIterationsTerminator
{
    public static AfterIterationsTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, int maximumIterations) => new(maximumIterations);
}

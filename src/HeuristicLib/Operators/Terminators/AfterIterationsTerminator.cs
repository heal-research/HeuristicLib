using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record AfterIterationsTerminator<TCandidate>
    : StatefulTerminator<TCandidate, AfterIterationsTerminator<TCandidate>.ExecutionState>
{
    public sealed class ExecutionState
    {
        public int CurrentCount { get; set; }
    }

    public AfterIterationsTerminator(int maximumIterations)
    {
        MaximumIterations = maximumIterations;
    }

    /// <summary>
    /// Gets the iteration limit. The expected value is positive.
    /// </summary>
    /// <remarks>A nonpositive limit terminates on the first checked state.</remarks>
    public int MaximumIterations { get; init; }

    protected override ExecutionState CreateInitialState() => new();

    protected override bool IsTerminalState(ExecutionState executionState)
    {
        executionState.CurrentCount += 1;
        return executionState.CurrentCount >= MaximumIterations;
    }
}

public static class AfterIterationsTerminator
{
    public static AfterIterationsTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, int maximumIterations) => new(maximumIterations);
}

using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

public record AfterIterationsTerminator<TCandidate>
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

    public int MaximumIterations
    {
        get;
        init => field = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MaximumIterations), "MaximumIterations must be positive.");
    }

    protected override ExecutionState CreateInitialState() => new();

    protected override bool IsTerminalState(ExecutionState executionState)
    {
        executionState.CurrentCount += 1;
        return executionState.CurrentCount >= MaximumIterations;
    }
}

public static class AfterIterationsTerminator
{
    public static AfterIterationsTerminator<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem, int maximumIterations)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new(maximumIterations);
}
